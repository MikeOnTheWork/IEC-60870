using System;
using System.IO;
using System.Net.Sockets;
using IEC60870.Enum;
using IEC60870.IE;
using IEC60870.IE.Base;
using IEC60870.Object;
using IEC60870.Utils;

namespace IEC60870.Connection
{
    public class Connection
    {
        private static readonly byte[] TestfrActBuffer = { 0x68, 0x04, 0x43, 0x00, 0x00, 0x00 };
        private static readonly byte[] TestfrConBuffer = { 0x68, 0x04, 0x83, 0x00, 0x00, 0x00 };
        private static readonly byte[] StartdtActBuffer = { 0x68, 0x04, 0x07, 0x00, 0x00, 0x00 };
        private static readonly byte[] StartdtConBuffer = { 0x68, 0x04, 0x0b, 0x00, 0x00, 0x00 };

        private readonly byte[] _buffer = new byte[255];
        private readonly BinaryReader _reader;

        private readonly ConnectionSettings _settings;

        private readonly BinaryWriter _writer;

        private bool _closed;
        private IOException _closedIoException;
        public ConnectionEventListener.ConnectionClosed ConnectionClosed = null;

        private RunTask _maxIdleTimeTimerFuture;

        private RunTask _maxTimeNoAckReceivedFuture;

        private RunTask _maxTimeNoAckSentFuture;

        private RunTask _maxTimeNoTestConReceivedFuture;

        public ConnectionEventListener.NewASdu NewASdu = null;

        private int _originatorAddress;

        private int _receiveSequenceNumber;
        private int _sendSequenceNumber;

        private int _acknowledgedReceiveSequenceNumber;
        private int _acknowledgedSendSequenceNumber;

        private readonly CountDownLatch _startdtactSignal;
        private CountDownLatch _startdtConSignal;

        public Connection(Socket socket, ConnectionSettings settings)
        {
            _settings = settings;

            var ns = new NetworkStream(socket);

            _writer = new BinaryWriter(ns);
            _reader = new BinaryReader(ns);

            _startdtactSignal = new CountDownLatch(1);

            var connectionReader = new ConnectionReader(this);
            connectionReader.Start();
        }

        public void Close()
        {
            if (!_closed)
            {
                _closed = true;

                try
                {
                    _writer.Close();
                }
                catch (Exception e)
                {
                    throw new IOException(e.Message);
                }

                try
                {
                    _reader.Close();
                }
                catch (Exception e)
                {
                    throw new IOException(e.Message);
                }
            }
        }

        public void StartDataTransfer(int timeout = 0)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("timeout may not be negative");
            }

            _startdtConSignal = new CountDownLatch(1);

            try
            {
                _writer.Write(StartdtActBuffer, 0, StartdtActBuffer.Length);
                _writer.Flush();
            }
            catch (Exception e)
            {
                throw new IOException(e.Message);
            }

            if (timeout == 0)
            {
                _startdtConSignal.Wait();
            }
            else
            {
                _startdtConSignal.Wait(timeout);
            }
        }

        public void WaitForStartDt(int timeout = 0)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("timeout may not be negative");
            }

            if (timeout == 0)
            {
                _startdtactSignal.Wait();
            }
            else
            {
                _startdtactSignal.Wait(timeout);
            }

            try
            {
                _writer.Write(StartdtConBuffer, 0, StartdtConBuffer.Length);
                _writer.Flush();
            }
            catch (Exception e)
            {
                throw new IOException(e.Message);
            }

            ResetMaxIdleTimeTimer();
        }

        public void Send(ASdu aSdu)
        {
            _acknowledgedReceiveSequenceNumber = _receiveSequenceNumber;
            var requestAPdu = new APdu(_sendSequenceNumber, _receiveSequenceNumber, APdu.ApciType.I_FORMAT, aSdu);
            _sendSequenceNumber = (_sendSequenceNumber + 1) % 32768;

            if (_maxTimeNoAckSentFuture != null)
            {
                _maxTimeNoAckSentFuture.Cancel();
                _maxTimeNoAckSentFuture = null;
            }

            if (_maxTimeNoAckReceivedFuture == null)
            {
                ScheduleMaxTimeNoAckReceivedFuture();
            }

            var length = requestAPdu.Encode(_buffer, _settings);
            _writer.Write(_buffer, 0, length);
            _writer.Flush();

            ResetMaxIdleTimeTimer();
        }

        private void SendSFormatPdu()
        {
            var requestAPdu = new APdu(0, _receiveSequenceNumber, APdu.ApciType.S_FORMAT, null);
            requestAPdu.Encode(_buffer, _settings);

            _writer.Write(_buffer, 0, 6);
            _writer.Flush();

            _acknowledgedReceiveSequenceNumber = _receiveSequenceNumber;

            ResetMaxIdleTimeTimer();
        }

        #region COMMANDS
        public void SendConfirmation(ASdu aSdu)
        {
            var cot = aSdu.GetCauseOfTransmission();

            if (cot == CauseOfTransmission.ACTIVATION)
            {
                cot = CauseOfTransmission.ACTIVATION_CON;
            }
            else if (cot == CauseOfTransmission.DEACTIVATION)
            {
                cot = CauseOfTransmission.DEACTIVATION_CON;
            }

            Send(new ASdu(aSdu.GetTypeIdentification(), aSdu.IsSequenceOfElements, cot, aSdu.IsTestFrame(),
                aSdu.IsNegativeConfirm(), aSdu.GetOriginatorAddress(), aSdu.GetCommonAddress(),
                aSdu.GetInformationObjects()));
        }

        public void SingleCommand(int commonAddress, int informationObjectAddress, IeSingleCommand singleCommand)
        {
            var cot = singleCommand.IsCommandStateOn()
                ? CauseOfTransmission.ACTIVATION
                : CauseOfTransmission.DEACTIVATION;

            var aSdu = new ASdu(TypeId.C_SC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {singleCommand}})
                });

            Send(aSdu);
        }

        public void SingleCommandWithTimeTag(int commonAddress, int informationObjectAddress,
            IeSingleCommand singleCommand, IeTime56 timeTag)
        {
            var cot = singleCommand.IsCommandStateOn()
                ? CauseOfTransmission.ACTIVATION
                : CauseOfTransmission.DEACTIVATION;

            var aSdu = new ASdu(TypeId.C_SC_TA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {singleCommand, timeTag}})
                });

            Send(aSdu);
        }

        public void DoubleCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeDoubleCommand doubleCommand)
        {
            var aSdu = new ASdu(TypeId.C_DC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {doubleCommand}})
                });

            Send(aSdu);
        }

        public void DoubleCommandWithTimeTag(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeDoubleCommand doubleCommand, IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_DC_TA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {doubleCommand, timeTag}})
                });

            Send(aSdu);
        }

        public void RegulatingStepCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeRegulatingStepCommand regulatingStepCommand)
        {
            var aSdu = new ASdu(TypeId.C_RC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {regulatingStepCommand}})
                });

            Send(aSdu);
        }

        public void RegulatingStepCommandWithTimeTag(int commonAddress, CauseOfTransmission cot,
            int informationObjectAddress, IeRegulatingStepCommand regulatingStepCommand, IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_RC_TA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {regulatingStepCommand, timeTag}})
                });

            Send(aSdu);
        }

        public void SetNormalizedValueCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeNormalizedValue normalizedValue, IeQualifierOfSetPointCommand qualifier)
        {
            var aSdu = new ASdu(TypeId.C_SE_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {normalizedValue, qualifier}})
                });

            Send(aSdu);
        }

        public void SetNormalizedValueCommandWithTimeTag(int commonAddress, CauseOfTransmission cot,
            int informationObjectAddress, IeNormalizedValue normalizedValue, IeQualifierOfSetPointCommand qualifier,
            IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_SE_TA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {normalizedValue, qualifier, timeTag}})
                });

            Send(aSdu);
        }

        public void SetScaledValueCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeScaledValue scaledValue, IeQualifierOfSetPointCommand qualifier)
        {
            var aSdu = new ASdu(TypeId.C_SE_NB_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {scaledValue, qualifier}})
                });

            Send(aSdu);
        }

        public void SetScaledValueCommandWithTimeTag(int commonAddress, CauseOfTransmission cot,
            int informationObjectAddress, IeScaledValue scaledValue, IeQualifierOfSetPointCommand qualifier,
            IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_SE_TB_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {scaledValue, qualifier, timeTag}})
                });

            Send(aSdu);
        }

        public void SetShortFloatCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeShortFloat shortFloat, IeQualifierOfSetPointCommand qualifier)
        {
            var aSdu = new ASdu(TypeId.C_SE_NC_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {shortFloat, qualifier}})
                });

            Send(aSdu);
        }

        public void SetShortFloatCommandWithTimeTag(int commonAddress, CauseOfTransmission cot,
            int informationObjectAddress, IeShortFloat shortFloat, IeQualifierOfSetPointCommand qualifier,
            IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_SE_TC_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {shortFloat, qualifier, timeTag}})
                });

            Send(aSdu);
        }

        public void BitStringCommand(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeBinaryStateInformation binaryStateInformation)
        {
            var aSdu = new ASdu(TypeId.C_BO_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {binaryStateInformation}})
                });

            Send(aSdu);
        }

        public void BitStringCommandWithTimeTag(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeBinaryStateInformation binaryStateInformation, IeTime56 timeTag)
        {
            var aSdu = new ASdu(TypeId.C_BO_TA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {binaryStateInformation, timeTag}})
                });

            Send(aSdu);
        }

        public void Interrogation(int commonAddress, CauseOfTransmission cot, IeQualifierOfInterrogation qualifier)
        {
            var aSdu = new ASdu(TypeId.C_IC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[] { new InformationObject(0, new[] { new InformationElement[] { qualifier } }) });

            Send(aSdu);
        }

        public void CounterInterrogation(int commonAddress, CauseOfTransmission cot,
            IeQualifierOfCounterInterrogation qualifier)
        {
            var aSdu = new ASdu(TypeId.C_CI_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[] { new InformationObject(0, new[] { new InformationElement[] { qualifier } }) });

            Send(aSdu);
        }

        public void ReadCommand(int commonAddress, int informationObjectAddress)
        {
            var aSdu = new ASdu(TypeId.C_RD_NA_1, false, CauseOfTransmission.REQUEST, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(informationObjectAddress,
                        new InformationElement[][] {})
                });

            Send(aSdu);
        }

        public void SynchronizeClocks(int commonAddress, IeTime56 time)
        {
            var io = new InformationObject(0, new[] { new InformationElement[] { time } });

            InformationObject[] ios = { io };

            var aSdu = new ASdu(TypeId.C_CS_NA_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, ios);

            Send(aSdu);
        }

        public void TestCommand(int commonAddress)
        {
            var aSdu = new ASdu(TypeId.C_TS_NA_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(0,
                        new[] {new InformationElement[] {new IeFixedTestBitPattern()}})
                });

            Send(aSdu);
        }

        public void TestCommandWithTimeTag(int commonAddress, IeTestSequenceCounter testSequenceCounter, IeTime56 time)
        {
            var aSdu = new ASdu(TypeId.C_TS_TA_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(0, new[] {new InformationElement[] {testSequenceCounter, time}})
                });

            Send(aSdu);
        }

        public void ResetProcessCommand(int commonAddress, IeQualifierOfResetProcessCommand qualifier)
        {
            var aSdu = new ASdu(TypeId.C_RP_NA_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(0,
                        new[] {new InformationElement[] {qualifier}})
                });

            Send(aSdu);
        }

        public void DelayAcquisitionCommand(int commonAddress, CauseOfTransmission cot, IeTime16 time)
        {
            var aSdu = new ASdu(TypeId.C_CD_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[] { new InformationObject(0, new[] { new InformationElement[] { time } }) });

            Send(aSdu);
        }

        public void ParameterNormalizedValueCommand(int commonAddress, int informationObjectAddress,
            IeNormalizedValue normalizedValue, IeQualifierOfParameterOfMeasuredValues qualifier)
        {
            var aSdu = new ASdu(TypeId.P_ME_NA_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {normalizedValue, qualifier}})
                });

            Send(aSdu);
        }

        public void ParameterScaledValueCommand(int commonAddress, int informationObjectAddress,
            IeScaledValue scaledValue,
            IeQualifierOfParameterOfMeasuredValues qualifier)
        {
            var aSdu = new ASdu(TypeId.P_ME_NB_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {scaledValue, qualifier}})
                });

            Send(aSdu);
        }

        public void ParameterShortFloatCommand(int commonAddress, int informationObjectAddress, IeShortFloat shortFloat,
            IeQualifierOfParameterOfMeasuredValues qualifier)
        {
            var aSdu = new ASdu(TypeId.P_ME_NC_1, false, CauseOfTransmission.ACTIVATION, false, false, _originatorAddress,
                commonAddress, new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {shortFloat, qualifier}})
                });

            Send(aSdu);
        }

        public void ParameterActivation(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeQualifierOfParameterActivation qualifier)
        {
            var aSdu = new ASdu(TypeId.P_AC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {qualifier}})
                });

            Send(aSdu);
        }

        public void FileReady(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeLengthOfFileOrSection lengthOfFile, IeFileReadyQualifier qualifier)
        {
            var aSdu = new ASdu(TypeId.F_FR_NA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, lengthOfFile, qualifier}})
                });

            Send(aSdu);
        }

        public void SectionReady(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeNameOfSection nameOfSection, IeLengthOfFileOrSection lengthOfSection, IeSectionReadyQualifier qualifier)
        {
            var aSdu = new ASdu(TypeId.F_SR_NA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(
                        informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, nameOfSection, lengthOfSection, qualifier}})
                });

            Send(aSdu);
        }

        public void CallOrSelectFiles(int commonAddress, CauseOfTransmission cot, int informationObjectAddress,
            IeNameOfFile nameOfFile, IeNameOfSection nameOfSection, IeSelectAndCallQualifier qualifier)
        {
            var aSdu = new ASdu(TypeId.F_SC_NA_1, false, cot, false, false, _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, nameOfSection, qualifier}})
                });

            Send(aSdu);
        }

        public void LastSectionOrSegment(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeNameOfSection nameOfSection, IeLastSectionOrSegmentQualifier qualifier, IeChecksum checksum)
        {
            var aSdu = new ASdu(TypeId.F_LS_NA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(
                        informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, nameOfSection, qualifier, checksum}})
                });

            Send(aSdu);
        }

        public void AckFileOrSection(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeNameOfSection nameOfSection, IeAckFileOrSectionQualifier qualifier)
        {
            var aSdu = new ASdu(TypeId.F_AF_NA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(
                        informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, nameOfSection, qualifier}})
                });

            Send(aSdu);
        }

        public void SendSegment(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeNameOfSection nameOfSection, IeFileSegment segment)
        {
            var aSdu = new ASdu(TypeId.F_SG_NA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress,
                new[]
                {
                    new InformationObject(informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, nameOfSection, segment}})
                });
            Send(aSdu);
        }

        public void SendDirectory(int commonAddress, int informationObjectAddress, InformationElement[][] directory)
        {
            var aSdu = new ASdu(TypeId.F_DR_TA_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(
                        informationObjectAddress, directory)
                });

            Send(aSdu);
        }

        public void QueryLog(int commonAddress, int informationObjectAddress, IeNameOfFile nameOfFile,
            IeTime56 rangeStartTime, IeTime56 rangeEndTime)
        {
            var aSdu = new ASdu(TypeId.F_SC_NB_1, false, CauseOfTransmission.FILE_TRANSFER, false, false,
                _originatorAddress, commonAddress, new[]
                {
                    new InformationObject(
                        informationObjectAddress,
                        new[] {new InformationElement[] {nameOfFile, rangeStartTime, rangeEndTime}})
                });

            Send(aSdu);
        }

        #endregion
        #region HELPER

        public void SetOriginatorAddress(int address)
        {
            if (address < 0 || address > 255)
            {
                throw new ArgumentException("Originator Address must be between 0 and 255.");
            }

            _originatorAddress = address;
        }

        private int GetSequenceNumberDifference(int x, int y)
        {
            var difference = x - y;
            if (difference < 0)
            {
                difference += 32768;
            }

            return difference;
        }

        public int GetNumUnconfirmedIPdusSent()
        {
            lock (this)
            {
                return GetSequenceNumberDifference(_sendSequenceNumber, _acknowledgedSendSequenceNumber);
            }
        }

        public int GetOriginatorAddress()
        {
            return _originatorAddress;
        }

        #endregion

        private void HandleReceiveSequenceNumber(APdu aPdu)
        {
            if (_acknowledgedSendSequenceNumber != aPdu.GetReceiveSeqNumber())
            {
                if (GetSequenceNumberDifference(aPdu.GetReceiveSeqNumber(), _acknowledgedSendSequenceNumber) >
                    GetNumUnconfirmedIPdusSent())
                {
                    throw new IOException("Got unexpected receive sequence number: " + aPdu.GetReceiveSeqNumber()
                                          + ", expected a number between: " + _acknowledgedSendSequenceNumber + " and "
                                          + _sendSequenceNumber);
                }

                if (_maxTimeNoAckReceivedFuture != null)
                {
                    _maxTimeNoAckReceivedFuture.Cancel();
                    _maxTimeNoAckReceivedFuture = null;
                }

                _acknowledgedSendSequenceNumber = aPdu.GetReceiveSeqNumber();

                if (_sendSequenceNumber != _acknowledgedSendSequenceNumber)
                {
                    ScheduleMaxTimeNoAckReceivedFuture();
                }
            }
        }

        private void ResetMaxIdleTimeTimer()
        {
            if (_maxIdleTimeTimerFuture != null)
            {
                _maxIdleTimeTimerFuture.Cancel();
                _maxIdleTimeTimerFuture = null;
            }

            _maxIdleTimeTimerFuture = PeriodicTaskFactory.Start(() =>
            {
                try
                {
                    _writer.Write(TestfrActBuffer, 0, TestfrActBuffer.Length);
                    _writer.Flush();
                }
                catch (Exception e)
                {
                    throw new IOException(e.Message);
                }

                ScheduleMaxTimeNoTestConReceivedFuture();
            }, _settings.MaxIdleTime);
        }

        private void ScheduleMaxTimeNoTestConReceivedFuture()
        {
            if (_maxTimeNoTestConReceivedFuture != null)
            {
                _maxTimeNoTestConReceivedFuture.Cancel();
                _maxTimeNoTestConReceivedFuture = null;
            }

            _maxTimeNoTestConReceivedFuture = PeriodicTaskFactory.Start(() =>
            {
                Close();
                ConnectionClosed?.Invoke(new IOException(
                    "The maximum time that no test frame confirmation was received (t1) has been exceeded. t1 = "
                    + _settings.MaxTimeNoAckReceived + "ms"));
            }, _settings.MaxTimeNoAckReceived);
        }

        private void ScheduleMaxTimeNoAckReceivedFuture()
        {
            if (_maxTimeNoAckReceivedFuture != null)
            {
                _maxTimeNoAckReceivedFuture.Cancel();
                _maxTimeNoAckReceivedFuture = null;
            }

            _maxTimeNoAckReceivedFuture = PeriodicTaskFactory.Start(() =>
            {
                Close();
                _maxTimeNoTestConReceivedFuture = null;
                ConnectionClosed?.Invoke(new IOException(
                    "The maximum time that no test frame confirmation was received (t1) has been exceeded. t1 = "
                    + _settings.MaxTimeNoAckReceived + "ms"));
            }, _settings.MaxTimeNoAckReceived);
        }

        private class ConnectionReader : ThreadBase
        {
            private readonly Connection _innerConnection;

            public ConnectionReader(Connection connection)
            {
                _innerConnection = connection;
            }

            public override void Run()
            {
                try
                {
                    var reader = _innerConnection._reader;
                    while (true)
                    {

                        if (reader.ReadByte() != 0x68)
                        {
                            throw new IOException("Message does not start with 0x68");
                        }

                        var aPdu = new APdu(reader, _innerConnection._settings);
                        switch (aPdu.GetApciType())
                        {
                            case APdu.ApciType.I_FORMAT:
                                if (_innerConnection._receiveSequenceNumber != aPdu.GetSendSeqNumber())
                                {
                                    throw new IOException("Got unexpected send sequence number: " +
                                                          aPdu.GetSendSeqNumber()
                                                          + ", expected: " + _innerConnection._receiveSequenceNumber);
                                }

                                _innerConnection._receiveSequenceNumber = (aPdu.GetSendSeqNumber() + 1) % 32768;
                                _innerConnection.HandleReceiveSequenceNumber(aPdu);

                                _innerConnection.NewASdu?.Invoke(aPdu.GetASdu());

                                var numUnconfirmedIPdusReceived = _innerConnection.GetSequenceNumberDifference(
                                    _innerConnection._receiveSequenceNumber,
                                    _innerConnection._acknowledgedReceiveSequenceNumber);
                                if (numUnconfirmedIPdusReceived > _innerConnection._settings.MaxUnconfirmedIPdusReceived)
                                {
                                    _innerConnection.SendSFormatPdu();
                                    if (_innerConnection._maxTimeNoAckSentFuture != null)
                                    {
                                        _innerConnection._maxTimeNoAckSentFuture.Cancel();
                                        _innerConnection._maxTimeNoAckSentFuture = null;
                                    }
                                }
                                else
                                {
                                    if (_innerConnection._maxTimeNoAckSentFuture == null)
                                    {
                                        _innerConnection._maxTimeNoAckSentFuture =
                                            PeriodicTaskFactory.Start(() =>
                                            {
                                                _innerConnection.SendSFormatPdu();
                                                _innerConnection._maxTimeNoAckSentFuture = null;
                                            }, _innerConnection._settings.MaxTimeNoAckSent);
                                    }
                                }

                                _innerConnection.ResetMaxIdleTimeTimer();
                                break;
                            case APdu.ApciType.STARTDT_CON:
                                _innerConnection._startdtConSignal?.CountDown();
                                _innerConnection.ResetMaxIdleTimeTimer();
                                break;
                            case APdu.ApciType.STARTDT_ACT:
                                _innerConnection._startdtactSignal?.CountDown();
                                break;
                            case APdu.ApciType.S_FORMAT:
                                _innerConnection.HandleReceiveSequenceNumber(aPdu);
                                _innerConnection.ResetMaxIdleTimeTimer();
                                break;
                            case APdu.ApciType.TESTFR_ACT:
                                try
                                {
                                    _innerConnection._writer.Write(TestfrConBuffer, 0, TestfrConBuffer.Length);
                                    _innerConnection._writer.Flush();
                                }
                                catch (Exception e)
                                {
                                    throw new IOException(e.Message);
                                }

                                _innerConnection.ResetMaxIdleTimeTimer();
                                break;
                            case APdu.ApciType.TESTFR_CON:
                                if (_innerConnection._maxTimeNoTestConReceivedFuture != null)
                                {
                                    _innerConnection._maxTimeNoTestConReceivedFuture.Cancel();
                                    _innerConnection._maxTimeNoTestConReceivedFuture = null;
                                }
                                _innerConnection.ResetMaxIdleTimeTimer();
                                break;
                            default:
                                throw new IOException("Got unexpected message with APCI Type: " + aPdu.GetApciType());

                        }
                    }
                }
                catch (IOException e)
                {
                    _innerConnection._closedIoException = e;
                }
                catch (Exception e)
                {
                    _innerConnection._closedIoException = new IOException(e.Message);
                }
                finally
                {
                    _innerConnection.ConnectionClosed?.Invoke(_innerConnection._closedIoException);
                    _innerConnection.Close();
                }
            }
        }
    }
}