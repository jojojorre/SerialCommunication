using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        private SerialPort serialPortArduino;
        private Timer timerOefening5;

        // Veilige variabelen voor temperatuur
        private float currentDesiredTemp = 0f;
        private float currentActualTemp = 0f;

        public Form1()
        {
            InitializeComponent();
            InitializeSerialPort();
            InitializeTimer();

            // Zorg ervoor dat de poort netjes sluit als de applicatie stopt
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeSerialPort()
        {
            serialPortArduino = new SerialPort();
            serialPortArduino.ReadTimeout = 1000;
            serialPortArduino.WriteTimeout = 1000;
        }

        private void InitializeTimer()
        {
            timerOefening5 = new Timer();
            timerOefening5.Interval = 1000; // 1 seconde
            timerOefening5.Tick += TimerOefening5_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;

                if (comboBoxBaudrate.Items.Count > 0)
                {
                    int index = comboBoxBaudrate.Items.IndexOf("115200");
                    comboBoxBaudrate.SelectedIndex = index >= 0 ? index : 0;
                }

                labelStatus.Text = "Niet verbonden";
                labelGewensteTemp.Text = "-";
                labelHuidigeTemp.Text = "-";

                tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            }
            catch (Exception) { }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabPageOefening5 && serialPortArduino.IsOpen)
            {
                timerOefening5.Start();
            }
            else
            {
                timerOefening5.Stop();
            }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                if (selected != null && comboBoxPoort.Items.Contains(selected))
                {
                    comboBoxPoort.SelectedItem = selected;
                }
                else if (comboBoxPoort.Items.Count > 0)
                {
                    comboBoxPoort.SelectedIndex = 0;
                }
            }
            catch (Exception) { }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (serialPortArduino.IsOpen)
            {
                DisconnectFromArduino();
            }
            else
            {
                ConnectToArduino();
            }
        }

        private void ConnectToArduino()
        {
            try
            {
                // Configureer alle port properties
                serialPortArduino.PortName = comboBoxPoort.SelectedItem.ToString();
                serialPortArduino.BaudRate = int.Parse(comboBoxBaudrate.SelectedItem.ToString());

                if (numericUpDownDatabits != null) serialPortArduino.DataBits = (int)numericUpDownDatabits.Value;

                // Stopbits instellen
                if (radioButtonStopbitsNone != null && radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                else if (radioButtonStopbitsOne != null && radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                else if (radioButtonStopbitsOnePointFive != null && radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                else if (radioButtonStopbitsTwo != null && radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;

                // Parity instellen
                if (radioButtonParityNone != null && radioButtonParityNone.Checked) serialPortArduino.Parity = Parity.None;
                else if (radioButtonParityOdd != null && radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                else if (radioButtonParityEven != null && radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                else if (radioButtonParityMark != null && radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                else if (radioButtonParitySpace != null && radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;

                // Handshake instellen
                if (radioButtonHandshakeNone != null && radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                else if (radioButtonHandshakeRTS != null && radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                else if (radioButtonHandshakeRTSXonXoff != null && radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                else if (radioButtonHandshakeXonXoff != null && radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;

                // RTS en DTR instellen
                if (checkBoxRtsEnable != null) serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;
                if (checkBoxDtrEnable != null) serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;
                else serialPortArduino.DtrEnable = true; // Zorg dat Arduino reset werkt

                // Open de verbinding
                serialPortArduino.Open();

                if (VerifyPingPong())
                {
                    UpdateUIOnConnect();
                    labelStatus.Text = "Verbonden met Arduino op " + comboBoxPoort.SelectedItem;
                }
                else
                {
                    serialPortArduino.Close();
                    MessageBox.Show("Arduino antwoordde niet correct op ping.", "Verbindingsfout");
                    labelStatus.Text = "Verbindingsfout: Geen pong antwoord";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij het openen van de seriële poort:\n" + ex.Message, "Verbindingsfout");
                labelStatus.Text = "Verbindingsfout: " + ex.Message;
            }
        }

        private bool VerifyPingPong()
        {
            try
            {
                serialPortArduino.DiscardInBuffer();
                serialPortArduino.WriteLine("ping");
                string response = serialPortArduino.ReadLine();
                return response != null && response.Trim().Equals("pong", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateUIOnConnect()
        {
            radioButtonVerbonden.Checked = true;
            buttonConnect.Text = "Disconnect";

            if (tabControl.SelectedTab == tabPageOefening5)
            {
                timerOefening5.Start();
            }
        }

        private void DisconnectFromArduino()
        {
            try
            {
                timerOefening5.Stop();
                if (serialPortArduino.IsOpen)
                {
                    serialPortArduino.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij het sluiten van de verbinding:\n" + ex.Message, "Fout");
            }
            finally
            {
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
                labelStatus.Text = "Niet verbonden";
                labelGewensteTemp.Text = "-";
                labelHuidigeTemp.Text = "-";
            }
        }

        private void TimerOefening5_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!serialPortArduino.IsOpen)
                {
                    DisconnectFromArduino();
                    return;
                }

                // 1. Ping check
                serialPortArduino.DiscardInBuffer();
                serialPortArduino.WriteLine("ping");
                string pingResp = serialPortArduino.ReadLine().Trim().ToLower();

                if (pingResp != "pong") return;

                // 2. Haal gewenste temperatuur (Potentiometer)
                serialPortArduino.WriteLine("temp");
                string tempRaw = serialPortArduino.ReadLine().Trim();
                if (tempRaw.Contains(":"))
                {
                    // VERBETERING: Vervang punt door komma voor correcte Nederlandse Windows formattering
                    string tempStr = tempRaw.Split(':')[1].Trim().Replace('.', ',');

                    if (float.TryParse(tempStr, out currentDesiredTemp))
                    {
                        labelGewensteTemp.Text = currentDesiredTemp.ToString("F1") + " °C";
                    }
                }

                // 3. Haal huidige temperatuur (LM35)
                serialPortArduino.WriteLine("currenttemp");
                string currentRaw = serialPortArduino.ReadLine().Trim();
                if (currentRaw.Contains(":"))
                {
                    // VERBETERING: Vervang punt door komma voor correcte Nederlandse Windows formattering
                    string tempStr = currentRaw.Split(':')[1].Trim().Replace('.', ',');

                    if (float.TryParse(tempStr, out currentActualTemp))
                    {
                        labelHuidigeTemp.Text = currentActualTemp.ToString("F1") + " °C";
                    }
                }

                // 4. LED control (Verwarming simulatie)
                if (currentActualTemp < currentDesiredTemp)
                {
                    serialPortArduino.WriteLine("set d2 high");
                    labelStatus.Text = "Verwarming AAN (LED D2 High)";
                }
                else
                {
                    serialPortArduino.WriteLine("set d2 low");
                    labelStatus.Text = "Verwarming UIT (LED D2 Low)";
                }

                try { serialPortArduino.ReadLine(); } catch { }
            }
            catch (TimeoutException)
            {
                labelStatus.Text = "Time-out bij communicatie.";
            }
            catch (Exception ex)
            {
                labelStatus.Text = "ERROR: " + ex.Message;
            }
        }

        // Zorgt ervoor dat de poort sluit als je het venster via het kruisje sluit
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPortArduino != null && serialPortArduino.IsOpen)
            {
                timerOefening5.Stop();
                serialPortArduino.Close();
            }
        }
    }
}