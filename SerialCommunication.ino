
#include "SerialCommand.h"
#include "analog.c"

#define SerialPort Serial
#define Baudrate 115200

// Temperature scaling constants for linear conversion
// Gewenste temperatuur (Potentiometer op A0): 5-45°C
#define TEMP_DESIRED_OFFSET 5.0
#define TEMP_DESIRED_SLOPE 40.0

// Huidige temperatuur (LM35 sensor op A1): 0-500°C
#define TEMP_CURRENT_OFFSET 0.0
#define TEMP_CURRENT_SLOPE 500.0

// ADC resolution (0-1023)
#define ADC_MAX 1023.0

int numberOfPings;
SerialCommand sCmd(SerialPort);

void setup() 
{
  digitalWrite(13, LOW);
  SerialPort.begin(Baudrate);

  sCmd.addCommand("set", onSet);
  sCmd.addCommand("toggle", onToggle);
  sCmd.addCommand("get", onGet);
  sCmd.addCommand("temp", onGetTemp);
  sCmd.addCommand("currenttemp", onGetCurrentTemp);
  sCmd.addCommand("ping", onPing);
  sCmd.addCommand("help", onHelp);
  sCmd.addCommand("debug", onDebug);
  sCmd.setDefaultHandler(onUnknownCommand);

  for (int i = 2; i < 5; i++) pinMode(i, OUTPUT);
  for (int i = 9; i < 12; i++) pinMode(i, OUTPUT);
  for (int i = 5; i < 8; i++) pinMode(i, INPUT);
  for (int i = A0; i <= A5; i++) pinMode(i, INPUT);

  analogReference(DEFAULT);
  
  //SerialPort.println(F("ready"));
}

void loop() 
{
  sCmd.readSerial();
}

void onUnknownCommand(char* cmd)
{
  SerialPort.print(F("unknown command \""));
  SerialPort.print(cmd);
  SerialPort.println(F("\""));
  sCmd.clearBuffer();
}

void onSet()
{
  char* arg1 = sCmd.next();
  char* arg2 = sCmd.next();

  if ((arg1 == NULL) || (arg2 == NULL)) SerialPort.println(F("Error: incorrect number of arguments"));
  else if (startsWith(arg1, "d"))
  {
    arg1 = &arg1[1];
    if (isValidNumber(arg1))
    {
      int pin = atoi(arg1);
      if ((pin >= 2) && (pin <= 4))
      {
        if ((strcmp(arg2, "high") == 0) || (strcmp(arg2, "on") == 0) || (strcmp(arg2, "1") == 0)) 
        {
          digitalWrite(pin, HIGH);
          SerialPort.println(F("set done"));
        }
        else if ((strcmp(arg2, "low") == 0) || (strcmp(arg2, "off") == 0) || (strcmp(arg2, "0") == 0))
        {
          digitalWrite(pin, LOW);
          SerialPort.println(F("set done"));       
        }
        else SerialPort.println(F("Error: invalid argument 2"));
      }
      else SerialPort.println(F("Error: invalid argument 1"));
    }
    else SerialPort.println(F("Error: invalid argument 1"));
    
    
  }
  else if (startsWith(arg1, "pwm"))
  {
    arg1 = &arg1[3];
    if (isValidNumber(arg1))
    {
      int pin = atoi(arg1);
      if ((pin >= 9) && (pin <= 11))
      {
        if (isValidNumber(arg2))
        {
          int value = atoi(arg2);
          if ((value >= 0) && (value <= 255))
          {
            analogWrite(pin, value);
            SerialPort.println(F("set done"));
          }
          else SerialPort.println(F("Error: invalid argument 2"));
          
        }
        else SerialPort.println(F("Error: invalid argument 2"));
      }
      else SerialPort.println(F("Error: invalid argument 1"));
    }
    else SerialPort.println(F("Error: invalid argument 1"));
  }
  else SerialPort.println(F("Error: invalid argument 1"));
}

void onToggle()
{
  char* arg1 = sCmd.next();

  if (arg1 == NULL) SerialPort.println(F("Error: incorrect number of arguments"));
  else if (startsWith(arg1, "d"))
  {
    arg1 = &arg1[1];
    if (isValidNumber(arg1))
    {
      int pin = atoi(arg1);
      if ((pin >= 2) && (pin <= 4))
      {
          digitalWrite(pin, not digitalRead(pin));
          SerialPort.println(F("toggle done"));
      }
      else SerialPort.println(F("Error: invalid argument 1"));
    }
    else SerialPort.println(F("Error: invalid argument 1"));
  }
  else SerialPort.println(F("Error: invalid argument 1"));
}

void onGet()
{
  char* arg1 = sCmd.next();

  if (arg1 == NULL) SerialPort.println(F("Error: incorrect number of arguments"));
  else if (startsWith(arg1, "d"))
  {
    arg1 = &arg1[1];
    if (isValidNumber(arg1))
    {
      int pin = atoi(arg1);
      if ((pin >= 2) && (pin <= 7))
      {
        SerialPort.print("d");
        SerialPort.print(pin);
        SerialPort.print(": ");
        SerialPort.println(digitalRead(pin));
      }
      else SerialPort.println(F("Error: invalid argument 1"));
    }
    else SerialPort.println(F("Error: invalid argument 1"));
  }
  else if (startsWith(arg1, "a"))
  {
    arg1 = &arg1[1];
    if (isValidNumber(arg1))
    {
      int pin = atoi(arg1);
      if ((pin >= 0) && (pin <= 5))
      {
        SerialPort.print("a");
        SerialPort.print(pin);
        SerialPort.print(": ");
        SerialPort.println(analogReadDelay(A0 + pin, 50000));        
      }
      else SerialPort.println(F("Error: invalid argument 1"));
    }
    else SerialPort.println(F("Error: invalid argument 1"));    
  }
  else SerialPort.println(F("Error: invalid argument 1"));
}

void onGetTemp()
{
  // Read desired temperature from potentiometer on A0
  // Linear scaling: 0-1023 ADC → 5-45°C
  // Formula: temp = (adc_value / 1023.0) * 40.0 + 5.0
  // See FAQ: "Analoge waardes herschalen"
  int adcValue = analogReadDelay(A0, 50000);
  float temperature = (adcValue / ADC_MAX) * TEMP_DESIRED_SLOPE + TEMP_DESIRED_OFFSET;
  SerialPort.print(F("temp: "));
  SerialPort.println(temperature);
}

void onGetCurrentTemp()
{
  // Read current temperature from LM35 sensor on A1
  // Linear scaling: 0-1023 ADC → 0-500°C
  // Formula: temp = (adc_value / 1023.0) * 500.0
  // See FAQ: "Analoge waardes herschalen - deel 2"
  int adcValue = analogReadDelay(A1, 50000);
  float temperature = (adcValue / ADC_MAX) * TEMP_CURRENT_SLOPE + TEMP_CURRENT_OFFSET;
  SerialPort.print(F("currenttemp: "));
  SerialPort.println(temperature);
}

void onPing()
{
  SerialPort.println(F("pong"));
  numberOfPings++;  
}

void onHelp()
{
  SerialPort.println(F("BZL opdracht seriële communicatie"));
  SerialPort.println();
  SerialPort.println(F("Commando's:"));
  SerialPort.println(F("\tset d[2..4] [0|1|on|off|high|low]"));
  SerialPort.println(F("\tset pwm[9..11] [0..255]"));
  SerialPort.println(F("\ttoggle d[2..4]"));
  SerialPort.println(F("\tget d[2..7]"));
  SerialPort.println(F("\tget a[0..5]"));
  SerialPort.println(F("\ttemp"));
  SerialPort.println(F("\tcurrenttemp"));
  SerialPort.println(F("\tping"));
  SerialPort.println(F("\tdebug"));  
  SerialPort.println(F("\thelp"));  
}

void onDebug()
{
  SerialPort.print(F("Je hebt "));
  SerialPort.print(numberOfPings);
  SerialPort.println(F(" keer ping pong gespeeld sinds de laatste reset"));
  SerialPort.println(F("De ping pong teller wordt nu gereset"));
  numberOfPings = 0;
}

boolean isValidNumber(char* str)
{
  int len = strlen(str);
  if ((len < 1) || (len > 3)) return false;
  else for (int i = 0; i < len; i++) if (!isDigit(str[i])) return false;
  return true;
} 

boolean startsWith(char* arg, char* str)
{
  int len = strlen(str);
  if (len >= strlen(arg)) return false;
  else for (int i = 0; i < len; i++) if (arg[i] != str[i]) return false;
  return true;
}
