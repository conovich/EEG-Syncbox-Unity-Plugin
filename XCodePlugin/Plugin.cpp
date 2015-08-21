/*
	This is a simple plugin, a bunch of functions that do simple things.
*/

#include "Plugin.pch"
#include "u3Extended.h"
#include <cstdlib> //for random()
#include <unistd.h> //for usleep(microseconds)

//labjackusb
extern "C" float LJUSB_GetLibraryVersion();

//u3Extended.c
extern "C" HANDLE openUSB(int ID);
extern "C" void closeUSB(HANDLE hDevice);
extern "C" void toggleHandleLEDOn(HANDLE hDevice, int channel);
extern "C" void toggleHandleLEDOff(HANDLE hDevice, int channel);
extern "C" void SetFIOState(HANDLE hDevice, int port, int state);
extern "C" int GetFIOState(HANDLE hDevice, int port);
extern "C" void configTC(HANDLE hDevice, int TimerCounterPinOffset, int NumberOfTimersEnabled, long *aEnableTimers, long *aEnableCounters, int tc_base , float divisor, long *aTimerModes, double *aTimerValues);


HANDLE hDevice;
bool isDeviceOpen = false;
bool isLightOn = false;
int stimChannel = 4;
int syncChannel = 0;
//random seed must be set once during the duration of the program
//random is used in sync pulse for time jitter
bool isRandomSeedSet = false;


const char* PrintHello(){
	return "Hello";
}

int PrintANumber(){
	return 8;
}

int AddTwoIntegers(int a, int b) {
	return a + b;
}

float AddTwoFloats(float a, float b) {
    //return a+b;
	return LJUSB_GetLibraryVersion();
}




//u3.c
const char* OpenUSB(){
    int localID;
    //long error;
    
    //Open first found U3 over USB
    localID = -1;
    
    hDevice = openUSB(localID);
    if(hDevice != NULL){
        isDeviceOpen = true;
        return "opened USB!";
    }
    else{
        isDeviceOpen = false;
        return "didn't open USB...";
    }

}

const char* CloseUSB(){
    //closeUSBConnection(myDeviceHandle);
    if(isDeviceOpen == true){
        isDeviceOpen = false;
        closeUSB(hDevice);
        return "closed USB!";
    }
    else{
        return "didn't close USB.";
    }
}

const char* TurnLEDOn(){
    if(isDeviceOpen){
        toggleHandleLEDOn(hDevice, stimChannel);
        return "Light On!";
    }
    return "No device to turn on LED.";
    
    
}

const char* TurnLEDOff(){
    if(isDeviceOpen){
        toggleHandleLEDOff(hDevice, stimChannel);
        return "Light Off!";
    }
    return "No device to turn off LED.";
}


//ex: a 10 ms pulse every second — until the duration is over...
float SyncPulse(){
    if(!isRandomSeedSet){
        //set random seed
        srand (static_cast <unsigned> (time(0)));
        isRandomSeedSet = true;
    }
    
    float pulseTimeSeconds = 0.5f; //TODO: make this a parameter???
    int pulseTimeMilliseconds = (int)(pulseTimeSeconds*1000);
    float maxTimeToWait = 1.0f - pulseTimeSeconds;
    
    float timeBeforePulseSeconds = static_cast <float> (rand()) / static_cast <float> (RAND_MAX/maxTimeToWait); //creates a random float between 0.0f and maxTimeToWait
    unsigned int timeBeforePulseMilliseconds = (int)(timeBeforePulseSeconds*1000);
    
    unsigned int microseconds = timeBeforePulseMilliseconds*1000;
    usleep(microseconds); //sleep for the random time to wait before the pulse
    
    
    //turn on pulse
    if(isDeviceOpen){
        toggleHandleLEDOn(hDevice, syncChannel);
    }
    
    //sleep for pulselength
    microseconds = pulseTimeMilliseconds*1000;
    usleep(microseconds); //sleep for the random time to wait before the pulse
    
    //turn off pulse
    if(isDeviceOpen){
        toggleHandleLEDOff(hDevice, syncChannel);
    }
    
    //#include <chrono>
    // ...
    //using namespace std::chrono;
    //long currentTimeMS = duration_cast< milliseconds >(system_clock::now().time_since_epoch());
    
    return timeBeforePulseSeconds;
}

const char* StimPulse(float durationSeconds, float freqHz, bool doRelay){
    
    doRelay = false;//doRelay = false?
    
    if(doRelay){
        //set relay 1 on
        //sleep (.3);
        //set relay 1 off
    }
    
    /*
     79          divisor     = int(250)
     80          clock_rate  = 1000000/divisor
     81          timer_val   = int(.5 * clock_rate / freqhz)
     82          num_cycles  = int(round(duration * freqhz))
     83*/
    
    float divisor = 250.0f;
    
    float clock_rate = 10000000.0f/divisor;
    
    int timerVal = (int)(0.5f * clock_rate / freqHz);
    double timerValDouble = (double)timerVal;
    
    int numCycles = (int)(round(durationSeconds * freqHz));
    
    /*
     84          # Set the timer clock base and divisor
     85
     86          tc_base     = 3; #48Mhz Clock
     87
     88          # print ''
     89          # print 'TimerClockBase = ', tc_base
     90          # print 'Divisor        = ', divisor
     91          # print 'clock_rate     = ', clock_rate
     92          # print 'num_cycles     = ', num_cycles
     93          # print 'timer_val      = ', timer_val
     94          # print ''
     95
     96*/
    
    int tc_base = 3; //48Mhz Clock
    
    
    
    /*
     97          # Only Allow 7.8125 Hz to 50 Hz
     98
     99          if freqhz < 7.8125:
     100              print 'freqhz Out of Range, Must be within 7.8125 and 50 Hz'
     101              sys.exit(0)
     102
     103          if freqhz > 50:
     104              print 'freqhz Out of Range, Must be within 7.8125 and 50 Hz'
     105              sys.exit(0)
     106
     107          # Only allow a certain range of duration
     108
     109          if duration < .1:
     110              print 'Duration Out of Range, Must be between .1 and 20 seconds'
     111              sys.exit(0)
     112
     113          if duration > 20:
     114              print 'Duration Out of Range, Must be between .1 and 20 seconds'
     115              sys.exit(0)
     116
     */
    
    if(freqHz < 7.8125 || freqHz > 50){ //if frequency is out of bounds
        //exit(0);
        return ("freqhz Out of Range, Must be within 7.8125 and 50 Hz.");
    }
    
    if(durationSeconds < .1f || durationSeconds > 20.0f){ //if duration is out of bounds
        //exit(0);
        return ("Duration Out of Range, Must be between .1 and 20 seconds");
    }

     
     
     /*
     117          # Set the timer/counter pin offset to 4, which will put the first
     118          # timer/counter on FIO4 and the second on FIO5.
     119
     120          self.configIO(TimerCounterPinOffset = 4, EnableCounter1 = True, EnableCounter0 = None, NumberOfTimersEnabled = 2, FIOAnalog = None, EIOAnalog = None, EnableUART = None)
     121
      */
    
    int TimerCounterPinOffset = 4;
    long EnableCounter1 = 1;
    long EnableCounter0 = 0;
    int NumberOfTimersEnabled = 2;
    int FIOAnalog = 0;  //TODO
    int EIOAnalog = 0;  //TODO
    bool EnableUART = false;    //TODO
    
    /*
     122          # Run at slower speed during debug, so LED flashing can be seen
     123          # Since we are using clock with divisor support, Counter0 is not available.
     124
     125          # Set the timer base
     126          self.configTimerClock( TimerClockBase = tc_base, TimerClockDivisor = divisor)
     127
     */
    
    
    /*
     128          # Configure timer1 for the number of pulses before stopping timer0
     129          timer_num   = 1
     130          mode        = 9
     131          self.writeRegister(7100+(2*timer_num), [mode, num_cycles])
     132   
     */
    
    /*
     133          # Configure timer0 for frequency mode 
     134          timer_num   = 0 
     135          mode        = 7 
     136          self.writeRegister(7100+(2*timer_num), [mode, timer_val])
     */
    
    
    //aEnableTimers = An array where each element specifies whether that timer is
    //                enabled.  Timers must be enabled in order starting from 0, so
    //                for instance, Timer1 cannot be enabled without enabling Timer
    //                0 also.  A nonzero for an array element specifies to enable
    //                that timer.  For the U3, this array must always have at least
    //                2 elements.
    long aEnableTimers[NumberOfTimersEnabled];
    for(int i = 0; i < NumberOfTimersEnabled; i++){
        aEnableTimers[i] = 1; //nonzero --> enabled timer
    }
    
                        
    //aEnableCounters = An array where each element specifies whether that counter
    //                  is enabled.  Counters do not have to be enabled in order
    //                  starting from 0, so Counter1 can be enabled when Counter0
    //                  is disabled.  A nonzero value for an array element
    //                  specifies to enable that counter.  For the U3, this array
    //                  must always have at least 2 elements.
    long aEnableCounters[2] = {EnableCounter0, EnableCounter1};
                        
    //aTimerModes = An array where each element is a constant specifying the mode
    //              for that timer.  For the U3, this array must always have at
    //              least 2 elements.
    long timer0Mode = 7;
    long timer1Mode = 9;
    long aTimerModes[2] = {timer0Mode, timer1Mode};
                        
    //aTimerValues = An array where each element specifies the initial value for
    //               that timer.  For the U3, this array must always have at least
    //               2 elements.
    double aTimerValues[2] = {timerValDouble,numCycles};
    
    
    configTC(hDevice, TimerCounterPinOffset, NumberOfTimersEnabled, aEnableTimers, aEnableCounters, tc_base, divisor, aTimerModes, aTimerValues);
    
    return "finished stim pulse!";
}

