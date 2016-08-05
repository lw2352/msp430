#include "wizepipes.h"

char ATMSG[MAX_BUFFER];//AT指令数组

//数据接收全局变量

char rbuffer[MAX_BUFFER];
char raddr=0,IsHaveCommand=0;
char IsReceiveCommand=0;

//发送计数


char netstatus=0;//联网状态,默认断开
char IsCaptureTime=0;//是否采样时间，此刻拒绝一切命令
char IsErase_ChipTime=0;

//GPS接收发送全局变量
char gpsrbuf[MAX_BUFFER];
char gpstbuf[MAX_BUFFER];
char isgpscmdok=0,gpsaddr=0,gpsstep=0;

WDATA wd1;
CDATA cd1;
TDATA td1;

unsigned long flash_addr;
unsigned int capdata;
unsigned short counter_tx;
unsigned long Time1=0,Time2,Timing;
unsigned long sum;


char timeout=0;
char capOK;
char step;
char ADdata[MAX_BUFFER];
char temp[MAX_BUFFER];
void main( void )
{
  //char i;
  //char *addr;
  InitialSystem();

  ReadConfig();  
  
  IE2 |= UCA0RXIE;//开启数据接收
  _EINT();//开启中断 
  
  delaymsec();//等待信息输出完成 
    
  
  //write_SegC();
  /*addr=(char *)0x1080;
  for(i=0;i<64;i++)
    ADdata[i]= *addr++;[22]*/
  //复位wifi模块
  /*
  IsHaveCommand =0;
  raddr=0; 
  strcpy(ATMSG,"AT+RST\r\n");
  senddata(ATMSG,strlen(ATMSG));
  OutTimer();
  
  while(IsHaveCommand!=C_READY && timeout!=1) _NOP();//等待响应指令
  delaymsec();//等待信息输出完成 
  */  
  

  
 //test flash
 /* MX29LV320t_Cmd_Erase_Chip(); 
  unsigned char test_byte[10];
  for(int i=0;i<10;i++)    
    MX29LV320t_Flash_Write(i,i);
   
  for(int i=0;i<10;i++)    
  test_byte[i]=MX29LV320t_Flash_Read(i);

 if(test_byte[0]==0xFF) MX29LV320t_Flash_Write(1,1);;
*/
  while(!netstatus)Connectwifi();
  sendheartbeat();
  
 
  //if(!netstatus)wd1.IsWifidefault=1;
 // ReadGPSInfo();
  StartTimer();
  while(1)
  {
  if(IsReceiveCommand==2)
    dotask();
  else if(IsCaptureTime)
    docapture();
  /*else if(IsErase_ChipTime)
  {
    MX29LV320t_Cmd_Reset();          //flash的芯片复位
    MX29LV320t_Cmd_Erase_Chip();     //将flash进行芯片擦除
    IsErase_ChipTime=0;
  }*/
 // else if( isgpscmdok==2)
    //ReadGPSInfo();
  
 // __bis_SR_register(CPUOFF);
  }

 //__bis_SR_register(CPUOFF);
}

// Timer A0 interrupt service routine
#pragma vector=TIMERA0_VECTOR
__interrupt void Timer_A (void)
{
  
    //暂停定时器
    //关闭时间定时器Timer A0
    CCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
    //关闭计数
    TACTL = MC_0; 
      
    _EINT();//开启中断
    Time1=(td1.hour*60+td1.minute);
    Time1=Time1*60+td1.seconds;
    Timing=ReadGPSInfo();
    if(Time1>Timing)
    {
       //Timing=ReadGPSInfo();
       Time1-=Timing;
       if(Time1<600)
       {
         IsCaptureTime=1;
         Time1*=10;
         Time1*=10;
         Time1*=10;
         step=0;
      
         TBCCTL0 = CCIE;                               // CCR0 interrupt enabled
         TBCCR0 = 999;
         TBCTL = TBSSEL_2 + MC_1 + ID_3;
       }
       else
      {
        IsHaveCommand=0;
        raddr=0;
        strcpy(ATMSG,"AT+CIPSTATUS\r\n");//查询IP状态
        senddata(ATMSG,strlen(ATMSG));
        while(!IsHaveCommand) _NOP();//等待响应指令 
       //delaymsec();
  
        if(rbuffer[7]!='3')//2 get ip 3 builded 4 lost ip
        {
         Connectwifi();
        } //网络断重连 
 
        //发送心跳包
        if(netstatus && IsReceiveCommand==0)sendheartbeat();
        CCTL0 = CCIE;                             // CCR0 interrupt enabled
        CCR0 = 0x1DFF;
        TACTL = TASSEL_1 + MC_1 + ID_3;                  // ACLK/8=512Hz, upmode
      }
    }
    if(Time1<=Timing)
    {
      IsHaveCommand=0;
      raddr=0;
      strcpy(ATMSG,"AT+CIPSTATUS\r\n");//查询IP状态
      senddata(ATMSG,strlen(ATMSG));
      while(!IsHaveCommand) _NOP();//等待响应指令 
     //delaymsec();
  
      if(rbuffer[7]!='3')//2 get ip 3 builded 4 lost ip
      {
       Connectwifi();
      } //网络断重连 
 
      //发送心跳包
      if(netstatus && IsReceiveCommand==0)sendheartbeat();
      CCTL0 = CCIE;                             // CCR0 interrupt enabled
      CCR0 = 0x1DFF;
      TACTL = TASSEL_1 + MC_1 + ID_3;                  // ACLK/8=512Hz, upmode
    }
  

    
    //SR |= 0x00F0;
    //SR &= 0xFFDF;
    
    //step=1;
  
   /* case 1:
     Time1++;
     if(netstatus==0)
       step=0;
     if(Time1==Timing)
     {
       //SR &= 0xFF0F;
       step=2;
       Time1=0;
     }
     break;
     
  case 2:
    //暂停定时器
    //关闭时间定时器Timer A0
    CCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
    //关闭计数
    TACTL = MC_0; 
    //Time2=td1.hour*3600+td1.minute*60+td1.seconds-ReadGPSInfo();
    Time2=300;
    step=3;

    //重启定时器
    CCTL0 = CCIE;                             // CCR0 interrupt enabled
    CCR0 = 0x01FF;
    TACTL = TASSEL_1 + MC_1 + ID_3;                  // ACLK/8=4096/8=512Hz, upmode
    break;
    
     case 3:
       Time2-=15;
       if(Time2<300)
       {
         IsErase_ChipTime=1;
         step=4;
       }     
      break;
      
  case 4:
    Time2-=15;
    if(Time2<280)
    {
      Time2*=1000;
      
      step=5;
    }
    break;
      
  case 5:
    Time2-=15000; 
     //暂停定时器
    //关闭时间定时器Timer A0
    CCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
    //关闭计数
    TACTL = MC_0; 
    
    step=6;
    
     CCTL0 = CCIE;                             // CCR0 interrupt enabled
     CCR0 = 999;
     TACTL = TASSEL_2 + MC_1 + ID_3;                  // SCLK/8=1MHz, upmode          
    break;
    
   case 6:
     Time1--;
     if(Time2==0)
     {
     //暂停定时器
    //关闭时间定时器Timer A0
    CCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
    //关闭计数
    TACTL = MC_0;
    IsCaptureTime=1;
     }
    break;*/  
}


#pragma vector=TIMERB0_VECTOR
__interrupt void Timer_B (void)
{
   switch(step)
   {
   case 0:
   Time1--;
   if(Time1==0)
   {
     TBCCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
     //关闭计数
     TBCCR0 = MC_0;
   }
   break;
   
   case 1:
     ADC12IE = 0x01;                           // Enable interrupt
     ADC12CTL0 |= ENC;                         // Conversion enabled
     ADC12CTL0 |= ADC12SC;
     sum+=1;
    break;
    
   case 2:
     timeout=1;
     TBCCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
     //关闭计数
     TBCTL = MC_0; 
     break;
   }
}

#pragma vector=ADC12_VECTOR
__interrupt void ADC12ISR (void)
{
   capdata = ADC12MEM0;
   ADdata[0]=capdata&0xFF;
   capdata>>=8;
   ADdata[1]=capdata&0x0F;
   
   MX29LV320t_Flash_Write(flash_addr,ADdata[1]);
   flash_addr++; 
   MX29LV320t_Flash_Write(flash_addr,ADdata[0]);
   flash_addr++;
   
   if(sum==300000)
   {
     sum=0;
     TBCCTL0 &= ~CCIE;     //关闭定时器B
     TBCTL &= ~MC_0;
     
     IE2 |= UCA0RXIE;
     _EINT();//开启中断 
     
     strcpy(ATMSG,"AT+CIPSEND=13\r\n");//发送连接指示
     senddata(ATMSG,strlen(ATMSG));
     while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
     _NOP();
     ATMSG[0]=0xA5;     
     ATMSG[1]=0xA5;     
     ATMSG[2]=0x22;     
     ATMSG[3]=cd1.ID[0];     
     ATMSG[4]=cd1.ID[1]; 
     ATMSG[5]=cd1.ID[2];     
     ATMSG[6]=cd1.ID[3];     
     ATMSG[7]=0x00;     
     ATMSG[8]=0x01;     
     ATMSG[9]=0x55;     
     ATMSG[10]=0xFF;     
     ATMSG[11]=0x5A;     
     ATMSG[12]=0x5A;     
     senddata(ATMSG,13);    
     while(IsHaveCommand!=C_OK) _NOP();//等待响应指令    
     StartTimer();
   }   
}

//接受WIFI模块中断处理程序
#pragma vector=USCIAB0RX_VECTOR
__interrupt void USCI0RX_ISR(void)
{
    rbuffer[raddr]=UCA0RXBUF; 
    
   // if(rbuffer[raddr-7]=='S' && rbuffer[raddr-6]=='T' && rbuffer[raddr-5]=='A' && rbuffer[raddr-4]=='T' && rbuffer[raddr-3]=='U' && rbuffer[raddr-2]=='S' && rbuffer[raddr-1]==':' && UCA0RXBUF=='4'){IsHaveCommand=C_UNLINK;raddr=0;netstatus=0;}//link is lost

    
    if(IsReceiveCommand==0)
    {
    if(rbuffer[raddr-3]=='O' && rbuffer[raddr-2]=='K' && rbuffer[raddr-1]=='\r' && UCA0RXBUF=='\n'){IsHaveCommand=C_OK;raddr=0;}//OK
    else if(rbuffer[raddr-6]=='r' && rbuffer[raddr-5]=='e' && rbuffer[raddr-4]=='a' && rbuffer[raddr-3]=='d' && rbuffer[raddr-2]=='y' && rbuffer[raddr-1]=='\r' && UCA0RXBUF=='\n'){IsHaveCommand=C_READY;raddr=0;}//ready
    else if(rbuffer[raddr-5]=='F' && rbuffer[raddr-4]=='A' && rbuffer[raddr-3]=='I' && rbuffer[raddr-2]=='L' && rbuffer[raddr-1]=='\r' && UCA0RXBUF=='\n'){IsHaveCommand=C_FAIL;netstatus=0;raddr=0;}//fail
    else if(rbuffer[raddr-6]=='E' && rbuffer[raddr-5]=='R' && rbuffer[raddr-4]=='R' && rbuffer[raddr-3]=='O' && rbuffer[raddr-2]=='R' && rbuffer[raddr-1]=='\r' && UCA0RXBUF=='\n'){IsHaveCommand=C_ERROR;raddr=0;}//error
    //else if(rbuffer[raddr-6]=='b' && rbuffer[raddr-5]=='u' && rbuffer[raddr-4]=='i' && rbuffer[raddr-3]=='l' && rbuffer[raddr-2]=='d' && rbuffer[raddr-1]=='e' && UCA0RXBUF=='d'){IsHaveCommand=C_BUILDED;raddr=0;}//link is builded
    //else if(rbuffer[raddr-4]=='w' && rbuffer[raddr-3]=='r' && rbuffer[raddr-2]=='o' && rbuffer[raddr-1]=='n' && UCA0RXBUF=='g'){IsHaveCommand=C_WRONG;netstatus=0;raddr=0;}//语法错误
    else if(UCA0RXBUF=='>'){IsHaveCommand=D_INPUT;}//>
    else if(rbuffer[raddr-3]=='+' && rbuffer[raddr-2]=='I' && rbuffer[raddr-1]=='P' && UCA0RXBUF=='D')
          {
               // IsHaveCommand=0;
                IsReceiveCommand=1;
                StopTimer();
          }//+IPD
    else raddr++;
    }

    if(IsReceiveCommand==1)
    {
      raddr++;
      if(UCA0RXBUF==':')
        raddr=0;  
      if(raddr>2)
      {
        if(rbuffer[raddr-2]==0x5A && UCA0RXBUF==0x5A) 
          IsReceiveCommand=2; 
      } 
    if(raddr==2)
      {
        if(rbuffer[0]!=0xA5 || rbuffer[1]!=0xA5) 
        {
          IsReceiveCommand=0;
          StartTimer();
        }
      }
      
    }
    if(raddr == MAX_BUFFER) {raddr=0;} 
 // __bis_SR_register(LPM0_bits); 
}

//接受GPS模块时间参数中断处理程序
#pragma vector=USCIAB1RX_VECTOR
__interrupt void USCI1RX_ISR(void)
{ 

  switch(gpsstep)
  {
    case 0:if(UCA1RXBUF=='$')
        {
          gpsstep=1;
          isgpscmdok=0;
          gpsaddr=0;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
        {
          gpsstep=0;
          gpsaddr=0;
        }
        break;
    case 1:if(UCA1RXBUF=='G')
        {
          gpsstep=2;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
          {gpsstep=0;gpsaddr=0;}
        break;
    case 2:if(UCA1RXBUF=='P')
        {
          gpsstep=3;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
          {gpsstep=0;gpsaddr=0;}
        break;       
    case 3:if(UCA1RXBUF=='G')
        {
          gpsstep=4;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
          {gpsstep=0;gpsaddr=0;}
        break;
    case 4:if(UCA1RXBUF=='G')
        {
          gpsstep=5;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
          {gpsstep=0;gpsaddr=0;}
        break; 
    case 5:if(UCA1RXBUF=='A')
        {
          gpsstep=6;
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        }
        else
          {gpsstep=0;gpsaddr=0;}
        break;
    case 6:if(UCA1RXBUF=='\n')
           {  
             gpsstep=0;
             isgpscmdok=2; 
           } 
          gpsrbuf[gpsaddr++]=UCA1RXBUF;
        break;   
  default:
    gpsstep=0;
    gpsaddr=0;
  }
}