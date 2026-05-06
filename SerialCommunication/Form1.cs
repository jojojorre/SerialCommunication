using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        private SerialPort serialPortArduino;
        private Timer timerOefening5;

        // Temperature scaling constants (must match Arduino values)
        // Gewenste temperatuur (Potentiometer A0): 5-45°C
        private const float TEMP_DESIRED_OFFSET = 5.0f;
        private const float TEMP_DESIRED_SLOPE = 40.0f;

        // Huidige temperatuur (LM35 sensor A1): 0-500°C
        private const float TEMP_CURRENT_OFFSET = 0.0f;
        private const float TEMP_CURRENT_SLOPE = 500.0f;

        // ADC resolution
        private const float ADC_MAX = 1023.0f;

        public Form1()
        {
            InitializeComponent();
            InitializeSerialPort();
            InitializeTimer();
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
            timerOefening5.Interval = 1000;
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

                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");

                // Register TabControl SelectionChanged event
                tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            }
            catch (Exception)
            { }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabPageOefening5)
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

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch (Exception)
            {
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
            }
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
                serialPortArduino.DataBits = (int)numericUpDownDatabits.Value;

                // Stopbits instellen
                if (radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                else if (radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                else if (radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                else if (radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;

                // Parity instellen
                if (radioButtonParityNone.Checked) serialPortArduino.Parity = Parity.None;
                else if (radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                else if (radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                else if (radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                else if (radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;

                // Handshake instellen
                if (radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                else if (radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                else if (radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;

                // RTS en DTR instellen
                serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;
                serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;

                // Open de verbinding
                serialPortArduino.Open();

                // Verzend ping en valideer pong
                if (VerifyPingPong())
                {
                    // Verbinding succesvol - update UI
                    UpdateUIOnConnect();
                    labelStatus.Text = "Verbonden met Arduino op " + comboBoxPoort.SelectedItem;
                }
                else
                {
                    // Ping/pong mislukt - sluit verbinding
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
                serialPortArduino.WriteLine("ping");
                string response = serialPortArduino.ReadLine();
                
                if (response != null && response.Trim().Equals("pong", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void UpdateUIOnConnect()
        {
            radioButtonVerbonden.Checked = true;
            buttonConnect.Text = "Disconnect";
        }

        private void DisconnectFromArduino()
        {
            try
            {
                serialPortArduino.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij het sluiten van de verbinding:\n" + ex.Message, "Fout");
            }
            finally
            {
                // Reset UI
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
                labelStatus.Text = "Verbroken";
            }
        }

        private void TimerOefening5_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!serialPortArduino.IsOpen)
                {
                    timerOefening5.Stop();
                    return;
                }

                float desiredTemp = 0;
                float currentTemp = 0;
                
                // Read desired temperature from potentiometer (A0: 5-45°C)
                // Linear scaling: 0-1023 ADC → 5-45°C
                // Formula: temp = (adc / 1023.0) * 40.0 + 5.0
                serialPortArduino.WriteLine("temp");
                string response = serialPortArduino.ReadLine();
                
                if (response != null && response.StartsWith("temp:"))
                {
                    string temperatureStr = response.Substring(5).Trim();
                    if (float.TryParse(temperatureStr, out desiredTemp))
                    {
                        labelGewensteTemp.Text = desiredTemp.ToString("F1") + " °C";
                    }
                }
                
                // Read current temperature from LM35 sensor (A1: 0-500°C)
                // Linear scaling: 0-1023 ADC → 0-500°C
                // Formula: temp = (adc / 1023.0) * 500.0
                serialPortArduino.WriteLine("currenttemp");
                response = serialPortArduino.ReadLine();
                
                if (response != null && response.StartsWith("currenttemp:"))
                {
                    string temperatureStr = response.Substring(12).Trim();
                    if (float.TryParse(temperatureStr, out currentTemp))
                    {
                        labelHuidigeTemp.Text = currentTemp.ToString("F1") + " °C";
                    }
                }
                
                // Control LED on pin 2: ON if current < desired, OFF otherwise
                if (currentTemp < desiredTemp)
                {
                    serialPortArduino.WriteLine("set d2 high");
                }
                else
                {
                    serialPortArduino.WriteLine("set d2 low");
                }
                
                // Read the response to clear the buffer
                serialPortArduino.ReadLine();
            }
            catch (ObjectDisposedException)
            {
                timerOefening5.Stop();
            }
            catch (InvalidOperationException)
            {
                timerOefening5.Stop();
            }
            catch (Exception ex)
            {
                // Log or handle unexpected exceptions
                labelStatus.Text = "Timer fout: " + ex.Message;
                timerOefening5.Stop();
            }
        }
    }
}
