//
//  u3Extended.c
//  XCodePlugin
//
//  Created by Corey Novich on 4/29/15.
//
//

#include "u3Extended.h"

/*new functions*/

float testFunct(){
    return 1;
}


HANDLE openUSB(int ID){
    HANDLE hDevice = NULL;
    hDevice = openUSBConnection(ID);
    
    return hDevice;
}

void closeUSB(HANDLE hDevice){
    closeUSBConnection(hDevice);
}

void toggleSyncBitOn(HANDLE hDevice){
    long error;
    
    //Read state of FIO4
    printf("\nCalling eDI to read the state of FIO4\n");
    long lngState;
    if((error = eDI(hDevice, 1, 4, &lngState)) != 0) //long eDI(HANDLE Handle, long ConfigIO, long Channel, long *State)
    goto end;
    printf("FIO4 state = %ld\n", lngState);
    
    //Toggle FIO4
    printf("\nCalling eDO to toggle FIO4\n");
    if((error = eDO(hDevice, 1, 4, !lngState)) != 0) //apparently setting the opposite lngState always turns it on? weird.
    goto end;
    
    end:
        if(error > 0)
            printf("Received an error code of %ld\n", error);
}

void toggleHandleLEDOn(HANDLE hDevice, int channel){
    long error;
    
    //Read state of FIO4
    printf("\nCalling eDI to read the state of FIO4\n");
    long lngState;
    if((error = eDI(hDevice, 1, channel, &lngState)) != 0)
        goto end;
    printf("FIO4 state = %ld\n", lngState);
    
    //Toggle FIO4
    printf("\nCalling eDO to toggle FIO4\n");
    if((error = eDO(hDevice, 1, channel, !lngState)) != 0) //apparently setting the opposite lngState always turns it on? weird.
        goto end;
    
    end:
        if(error > 0)
            printf("Received an error code of %ld\n", error);
    
}

void toggleHandleLEDOff(HANDLE hDevice, int channel){
    long error;
    
    //Read state of FIO4
    printf("\nCalling eDI to read the state of FIO4\n");
    long lngState;
    if((error = eDI(hDevice, 1, channel, &lngState)) != 0)
        goto end;
    printf("FIO4 state = %ld\n", lngState);
    
    //Toggle FIO4
    printf("\nCalling eDO to toggle FIO4\n");
    if((error = eDO(hDevice, 1, channel, lngState)) != 0) //apparently keeping the same lngState turns it off. weird.
        goto end;
    
end:
    if(error > 0)
        printf("Received an error code of %ld\n", error);
    
}

void SetFIOState(HANDLE hDevice, int port, int state){
    long lngState;
    long error = eDO(hDevice, 1, 4, lngState);
    
    if(error > 0){
        printf("Received an error code of %ld\n", error);
    }
}

int GetFIOState(HANDLE hDevice, int port){
    long lngState;
    long error = eDI(hDevice, 1, 4, &lngState);
    
    if(error > 0){
        printf("Received an error code of %ld\n", error);
    }
    
    return lngState;
}

//config timers and counters!
void configTC(HANDLE hDevice, int TimerCounterPinOffset, int NumberOfTimersEnabled, long *aEnableTimers, long *aEnableCounters, int tc_base , float divisor, long *aTimerModes, double *aTimerValues){
    
    //aEnableTimers = An array where each element specifies whether that timer is
    //                enabled.  Timers must be enabled in order starting from 0, so
    //                for instance, Timer1 cannot be enabled without enabling Timer
    //                0 also.  A nonzero for an array element specifies to enable
    //                that timer.  For the U3, this array must always have at least
    //                2 elements.

    
    
    //aEnableCounters = An array where each element specifies whether that counter
    //                  is enabled.  Counters do not have to be enabled in order
    //                  starting from 0, so Counter1 can be enabled when Counter0
    //                  is disabled.  A nonzero value for an array element
    //                  specifies to enable that counter.  For the U3, this array
    //                  must always have at least 2 elements.

    //aTimerModes = An array where each element is a constant specifying the mode
    //              for that timer.  For the U3, this array must always have at
    //              least 2 elements.
    
    
    //aTimerValues = An array where each element specifies the initial value for
    //               that timer.  For the U3, this array must always have at least
    //               2 elements.
    
    
    //Reserved (1&2) =  Pass 0.
    long Reserved1 = 0;
    long Reserved2 = 0;
    
    
    eTCConfig(hDevice, aEnableTimers, aEnableCounters, TimerCounterPinOffset, tc_base, divisor, aTimerModes, aTimerValues, Reserved1, Reserved2);

}


void configIO(HANDLE hDevice, int TimerCounterPinOffset, bool EnableCounter1, bool EnableCounter0, int NumberOfTimersEnabled, int FIOAnalog, int EIOAnalog, bool EnableUART){
   /* ehConfigIO( hDevice,
               uint8 inWriteMask,
               uint8 inTimerCounterConfig,
               uint8 inDAC1Enable,
               inFIOAnalog,
               inEIOAnalog,
               uint8 *outTimerCounterConfig,
               uint8 *outDAC1Enable,
               uint8 *outFIOAnalog,
               uint8 *outEIOAnalog);*/
}