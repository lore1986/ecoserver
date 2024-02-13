using System.Runtime.InteropServices;

namespace webapi.Utilities
{
    public class cmdRW
    {
        public const byte REQUEST_CMD1 = 0;
        public const byte RESPONSE_CMD1 = 1;
        public const byte DEBUGMES_CMD1 = 2;

        public const byte ID_MODULO_BASE = 0;
        public const byte ID_ALTO_LIVELLO = 1;
        public const byte ID_INTERFACCIA = 2;
        public const byte ID_WEBAPP = 3;

        public const byte ID_MUX_MPPT = 34;
        public const byte ID_RADIOCOMANDO = 35;

        public const byte ID_MPPT = 36;

        public const byte ID_IMU = 37;
        public const byte ID_ECHO = 38;
        public const byte ID_MOTORI = 39;
        public const byte ID_BMS = 40;
        public const byte ID_GPS = 41;
        public const byte ID_POWER = 42;
        public const byte ID_LED = 43;
        public const byte ID_MICRO_JETSON = 44;

        public const byte ID_MODULO_AMB = 50;
        public const byte ID_ROBOT_ARM_1 = 51;
        public const byte ID_ROBOT_ARM_2 = 52;
        public const byte ID_INCUBATORE = 53;
        public const byte ID_OPENER = 54;
        public const byte ID_QUANTITRAY = 55;

        public const byte ID_PRUA = 60;
        public const byte ID_POPPA = 70;

        public const byte ID_PORTA_0 = 0;
        public const byte ID_PORTA_1 = 1;
        public const byte ID_PORTA_5 = 5;
        public const byte ID_PORTA_6 = 6;
        public const byte ID_PORTA_7 = 7;
        public const byte ID_PORTA_8 = 8;

        public const byte ID_PORTA_SOCK = 9;

        /*        public const   byte  PORTA_0 Serial
                #if defined (ARDUINO_TEENSY41)
                public const   byte  PORTA_1 Serial1
                public const   byte  PORTA_5 Serial5
                public const   byte  PORTA_6 Serial6
                public const   byte  PORTA_7 Serial7
                public const   byte  PORTA_8 Serial8
                #endif
        */

        public const byte TEST_GENERIC_CMD2 = 254; //Comando per mandare un generico test dall'interfaccia
        public const byte TEST_GENERIC_CMD3 = 254; //Comando per mandare un generico test dall'interfaccia

        //*******************************MODULO BASE
        public const byte EN_SLEEP_CMD2 = 0;
        public const byte SET_DEBUG_PORT_CMD2 = 1;
        public const byte SET_SD_CMD2 = 2;
        public const byte SET_FLASH_CMD2 = 3;
        public const byte SAVE_MISSION_CMD2 = 4;
        public const byte GET_MISSION_CMD2 = 5;
        public const byte SEND_SCHEDULE_CMD2 = 6;
        public const byte READ_SCHEDULE_CMD2 = 7;
        public const byte SEND_DUMMY_CMD2 = 8;
        public const byte START_NEXT_SCHED_CMD2 = 9;
        public const byte UPDATE_MISS_LIST_CMD2 = 10;
        public const byte START_THIS_MISS_CMD2 = 11;
        public const byte SET_MP_RADIUS_CMD2 = 12;
        public const byte EN_DEB_PRINT_CMD2 = 13;
        public const byte LOCK_MUX_CH_CMD2 = 14;
        public const byte REMOTE_CONTROL_CMD2 = 15;
        public const byte SET_NAV_GPS_CMD2 = 16;
        public const byte SET_NAV_HEAD_CMD2 = 17;
        public const byte JS_DRIVING_SET_CMD2 = 18;
        public const byte JS_DRIVING_DATA_CMD2 = 19;
        public const byte RADIO_DRIVING_SET_CMD2 = 20;
        public const byte RADIO_DRIVING_DATA_CMD2 = 21;
        public const byte SET_TEL_NAV_CMD2 = 22;
        public const byte SET_AUT_NAV_CMD2 = 23;
        public const byte SET_MODE_TETAD_CMD2 = 24;
        public const byte SET_MODE_VELD_CMD2 = 25;
        public const byte SET_MISC_PARAM_CMD2 = 26;
        public const byte SET_JS_DEB_CMD2 = 27;
        public const byte GET_CONTROL_INFO_CMD2 = 28;
        public const byte GET_ANTICOLL_TETAD_CMD2 = 29;
        public const byte GET_JETSON_SIGNAL_CMD2 = 30;
        public const byte SEND_DATA_JETSON_CMD2 = 31;
        public const byte GET_JETSON_WP_CMD2 = 32;
        public const byte SET_DRONE_EQUIP_CMD2 = 33;
        public const byte GET_IP_PORT_CMD2 = 34;
        public const byte SET_IP_PORT_CMD2 = 35;
        public const byte COOLING_SET_CMD2 = 36;
        public const byte REBOOT_TEENSY_CMD2 = 37;
        public const byte MAIN_DATA_CMD2 = 38;

        public const byte SD_SET_DEB_TIME_CMD3 = 0;
        public const byte SD_READ_CMD3 = 1;
        public const byte SD_DELETE_CMD3 = 2;

        public const byte FLASH_READ_CMD3 = 0;
        public const byte FLASH_DELETE_CMD3 = 1;

        public const byte SAVE_MISSION_PARAM_CMD3 = 0;
        public const byte SAVE_MISSION_WP_CMD3 = 1;

        public const byte GET_MISSION_PARAM_CMD3 = 0;
        public const byte GET_MISSION_WP_CMD3 = 1;

        public const byte START_UPDATE_LIST_CMD3 = 0;
        public const byte UPDATE_DIR_LIST_CMD3 = 1;
        public const byte UPDATE_FILE_LIST_CMD3 = 2;
        public const byte END_FILE_LIST_CMD3 = 3;
        public const byte INPUT_JOYSTICK_CMD3 = 0;
        public const byte INPUT_RADIO_CMD3 = 1;

        public const byte JS_BOAT_CMD3 = 0;
        public const byte JS_AIR_DRONE_CMD3 = 1;
        public const byte JS_SUBM_CMD3 = 2;
        public const byte JS_ARM_CMD3 = 3;

        //*******************************POWER
        public const byte POWER_EN_CMD2 = 0;

        public const byte POWER_EN_GET_CMD3 = 0;
        public const byte POWER_EN_SET_CMD3 = 1;
        //*********************************IMU
        public const byte IMU_SET_CMD2 = 0;
        public const byte IMU_GET_CMD2 = 1;
        public const byte IMU_CFG_CMD2 = 2;
        public const byte IMU_DEB_CFG_CMD2 = 3;
        public const byte IMU_086_SRESET_CMD2 = 4;
        public const byte IMU_086_HRESET_CMD2 = 5;

        public const byte IMU_RPY_CMD3 = 0;
        public const byte IMU_RPY_ACC_CMD3 = 1;
        public const byte IMU_RPY_ACC_GYR_CMD3 = 2;
        public const byte IMU_RPY_ACC_GYR_MAG_CMD3 = 3;
        public const byte IMU_GET_086_CAL_CMD3 = 4;

        public const byte IMU_086_SET_REPORTS_CMD3 = 4;
        public const byte IMU_086_REQ_CAL_STA_CMD3 = 5;
        public const byte IMU_086_DEB_MES_EN_CMD3 = 6;

        public const byte IMU_UPDATE_CFG_GET_CMD3 = 0;
        public const byte IMU_UPDATE_CFG_SET_CMD3 = 1;
        public const byte IMU_UPDATE_CAL_GET_CMD3 = 2;
        public const byte IMU_UPDATE_CAL_SET_CMD3 = 3;

        public const byte IMU_DEB_CFG_GET_CMD3 = 0;
        public const byte IMU_DEB_CFG_SET_CMD3 = 1;

        //******************************MOTORI
        public const byte MOTOR_DRIVE_CMD2 = 0;
        public const byte MOTOR_TELEM_CMD2 = 1;

        public const byte MOTOR_OFF_CMD3 = 0;
        public const byte MOTOR_ERPM_CMD3 = 1;
        public const byte MOTOR_CURRENT_CMD3 = 2;
        public const byte MOTOR_ERPM_ALL_CMD3 = 3;

        public const byte MOTOR_TELEM_CDCS_CMD3 = 0;
        public const byte MOTOR_TELEM_DDSS_CMD3 = 1;

        public const byte MOTOR_CD = 1;
        public const byte MOTOR_CS = 2;
        public const byte MOTOR_DD = 3;
        public const byte MOTOR_SS = 4;

        public const byte INDEX_MOT_DD = 0;
        public const byte INDEX_MOT_SS = 1;
        public const byte INDEX_MOT_CD = 2;
        public const byte INDEX_MOT_CS = 3;

        public const byte AXIS_X = 0;
        public const byte AXIS_Y = 1;
        public const byte THROTTLE = 2;
        public const byte WHEEL = 3;

        //*********************************BMS

        public const byte BMS_PARAM_CMD2 = 0;
        public const byte BMS_GET_DATA_CMD2 = 1;
        public const byte BMS_DEB_CFG_CMD2 = 2;

        public const byte BMS_GET_VCELL_CMD3 = 0;
        public const byte BMS_GET_BASIC_CMD3 = 1;
        public const byte BMS_GET_EEPROM_CMD3 = 2;

        public const byte BMS_GET_PARAM_CMD3 = 0;
        public const byte BMS_SET_PARAM_CMD3 = 1;

        public const byte BMS_DEB_CFG_GET_CMD3 = 0;
        public const byte BMS_DEB_CFG_SET_CMD3 = 1;

        //*********************************GPS

        public const byte GPS_GET_CMD2 = 0;
        public const byte GPS_SET_CMD2 = 1;
        public const byte GPS_DEB_CFG_CMD2 = 2;

        public const byte GPS_NAV_PVT_CMD3 = 0;
        public const byte GPS_NAV_RELPOSNED_CMD3 = 1;

        public const byte GPS_DEB_CFG_GET_CMD3 = 0;
        public const byte GPS_DEB_CFG_SET_CMD3 = 1;

        //*********************************ECHO
        public const byte ECHO_NANO_GET_CMD2 = 0;

        public const byte ECHO_GET_CMD2 = 0;
        public const byte ECHO_CFG_CMD2 = 1;
        public const byte ECHO_DEB_CFG_CMD2 = 2;

        public const byte ECHO_CFG_GET_CMD3 = 0;
        public const byte ECHO_CFG_SET_CMD3 = 1;

        public const byte ECHO_DEB_CFG_GET_CMD3 = 0;
        public const byte ECHO_DEB_CFG_SET_CMD3 = 1;

        //*********************************LED
        public const byte LED_BULLET_CMD2 = 0;
        public const byte LED_MAIN_CMD2 = 1;
        public const byte LED_FRONT_CMD2 = 2;
        public const byte LED_IR_CMD2 = 3;
        public const byte LIGHT_SENS_CMD2 = 4;
        public const byte LED_FAIRING_CMD2 = 5;
        public const byte LED_FAIRING_DEB_CMD2 = 6;

        //*********************************MPPT
        public const byte SINC_CHAR_MPPT_0 = 36;
        public const byte SINC_CHAR_MPPT_1 = 36;
        public const byte SINC_CHAR_MPPT_2 = 69;

        public const byte id_mppt = 36;//SINC_CHAR_MPPT_0;
        public const byte MPPT_CMD1 = 0;

        public const byte MPPT_NANO_GET_CMD2 = 0;
        public const byte MPPT_GET_CMD2 = 1;
        public const byte MPPT_SET_CMD2 = 2;
        public const byte MPPT_DEB_CMD2 = 3;

        public const byte MPPT_DEB_GET_CMD3 = 0;
        public const byte MPPT_DEB_SET_CMD3 = 1;

        public const byte MPPT_0_GET_CMD3 = 0;
        public const byte MPPT_1_GET_CMD3 = 1;
        public const byte MPPT_2_GET_CMD3 = 2;
        public const byte MPPT_3_GET_CMD3 = 3;

        public const byte VI_CMD2 = 5;

        public const byte DIS_VI_NF_CMD3 = 0;
        public const byte EN_VI_NF_CMD3 = 1;


        public const byte REGI_VAL_CMD1 = 1;
        public const byte SET_BATT_CMD2 = 12;
        public const byte SET_BATT_NCELL_CMD3 = 4;

        //*********************************ricevente
        public const byte N_CH_READ = 6;

        public const byte CH_DX_X = 0;
        public const byte CH_DX_Y = 1;

        public const byte CH_SX_Y = 2;
        public const byte CH_SX_X = 3;

        public const byte CH_SX_ROT = 4;
        public const byte CH_DX_ROT = 5;
        //*********************************MODULO AMB
        public const byte REG_NEW_WP_SD_CMD2 = 0;
        public const byte READ_SD_WP_FILE_CMD2 = 1;
        //public const   byte  SET_SD_CMD2            2  // In comune con modulo base e interfaccia
        public const byte SET_AMB_POWER_CMD2 = 3;
        public const byte REG_ACTION_CMD2 = 4;
        public const byte REG_HEADER_CMD2 = 5;
        public const byte READ_SAMPLING_CMD2 = 6;
        public const byte MISSION_EXEC_CMD2 = 7;
        //public const   byte  UPDATE_MISS_LIST_CMD2     10 // In comune con modulo base e interfaccia
        //public const   byte  START_SERVO_MISSION_CMD2  12 // In comune fra modulo ambientale, interfaccia, bracci



        //public const   byte  SET_DRONE_EQUIP_CMD2      33 // In comune con modulo base e interfaccia
        //public const   byte  REBOOT_TEENSY_CMD2        37 // In comune con modulo base e interfaccia
        //public const   byte  MAIN_DATA_CMD2            38 // In comune con modulo base e interfaccia
        //*********************************ROBOT ARM
        public const byte GET_DATA_ARM_CMD2 = 0;
        public const byte SERVO_DRIVE_PER_CMD2 = 1;
        public const byte SERVO_DRIVE_ANG_CMD2 = 2;
        public const byte NEW_POINTS_CMD2 = 3;
        public const byte REG_STRUCT_MIN_CMD2 = 4;
        public const byte REG_STRUCT_MAX_CMD2 = 5;
        public const byte GO_TO_RIF_VITE_CMD2 = 6;
        public const byte GO_TO_ZERO_VITE_CMD2 = 7;
        public const byte SET_CURR_CLAW_CMD2 = 8;
        public const byte SERVO_GET_PER_CMD2 = 9;
        public const byte SERVO_GET_ANG_CMD2 = 10;
        public const byte SET_PID_PARAM_VITE_CMD2 = 11;
        //********************************* INCUBATORE
        public const byte GET_DATA_INC_CMD2 = 0;
        public const byte SET_TEMP_RIF_CMD2 = 1;
        public const byte DOOR_APRI_CHIUDI_CMD2 = 2;
        public const byte TOGGLE_BACKLIGHT_CMD2 = 3;
        public const byte SLEEP_INCUBATOR_CMD2 = 4;

        public const byte START_INCUB_MISSION_CMD2 = 15;

        public const byte SET_TEMP_RIF_UP_CMD3 = 0;
        public const byte SET_TEMP_RIF_DW_CMD3 = 1;

        public const byte DOOR_APRI_UP_CMD3 = 0;
        public const byte DOOR_APRI_DW_CMD3 = 1;
        public const byte DOOR_CHIUDI_UP_CMD3 = 2;
        public const byte DOOR_CHIUDI_DW_CMD3 = 3;
        public const byte DOOR_STOP_CMD3 = 4;
        //********************************* APRIBARATTOLO
        public const byte OPEN_JAR_CMD2 = 0;
        public const byte CLOSE_JAR_CMD2 = 1;
        //********************************* QUANTITRAY
        public const byte MOVE_JARS_CMD2 = 0;
        public const byte MOVE_PISTON_CMD2 = 1;
        public const byte MOVE_MOTOR_CMD2 = 2;

        public const byte RETRACT_PISTON_CMD3 = 0;
        public const byte PUSH_PISTON_CMD3 = 1;
        public const byte STOP_PISTON_CMD3 = 2;

        public const byte MOVE_MOTOR_CW_CMD3 = 0;
        public const byte MOVE_MOTOR_CCW_CMD3 = 1;
        public const byte STOP_MOTOR_CMD3 = 2;
        //********************************* PRUA
        public const byte GET_PRESSURE_CMD2 = 0;
        public const byte SET_PUMP_PARAM_CMD2 = 1;
        public const byte SET_COMPRESSOR_CMD2 = 2;
        public const byte SET_VALVES_CMD2 = 3;
        public const byte OPEN_MOD_DOOR_CMD2 = 4;
        public const byte CLOSE_MOD_DOOR_CMD2 = 5;
        public const byte SET_DEB_PRESS_CMD2 = 6;
        public const byte SET_GAV_PARAMS_CMD2 = 7;
        public const byte BOAT_OVERTURN_CMD2 = 8;
        public const byte EN_MODULE_BMS_CMD2 = 9;
        //****************************Indici per salvare lo stato di comunicazione
        public const byte INDEX_PINGPONG_MISSION = 0;
        //****************************Per l'albero dei file della flash
        public const byte END_OF_STRING = 0;
        public const byte DIRECTORY_CMD = 1;
        public const byte FILE_CMD = 2;
        public const byte PARENT_CMD = 3;
        //****************************struttura messaggio Inviato
        public const byte INDEX_SINCHAR_0 = 0;
        public const byte INDEX_SINCHAR_1 = 1;
        public const byte INDEX_SINCHAR_2 = 2;

        public const byte INDEX_BUF_LENG = 3;
        public const byte INDEX_BUF_SORG = 4;
        public const byte INDEX_BUF_DEST = 5;
        public const byte INDEX_BUF_ID_D = 6;

        public const byte INDEX_BUF_CMD_1 = 7;
        public const byte INDEX_BUF_CMD_2 = 8;
        public const byte INDEX_BUF_CMD_3 = 9;
        public const byte INDEX_BUF_CONTB = 10;

        public const byte MYID = ID_WEBAPP;


    }
}
