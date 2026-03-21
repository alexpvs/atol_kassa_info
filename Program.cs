/*
 * Created by SharpDevelop.
 * User: alex
 * Date: 06.11.2025
 * Time: 20:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Collections.Generic;
using Atol.Drivers10.Fptr;

namespace atol_kassa_info
{
	
	class Atol_kassa_info
	{
		
		const string ConnectionTypeCom = "COM";
		const string ConnectionTypeUsb = "USB"; 
		
		[STAThread]
		static void Main(string[] args)
		{

			if (args.Length == 0) {
				Console.WriteLine("add --help or /? to get info about usage");
				return;
			}

			if (Atol_kassa_info.FindHelpInArgs(args)) {
				return;
			}

			if (Atol_kassa_info.FindVersionInArgs(args)) {
				return;
			}

			if (Atol_kassa_info.FindCreatyEmptyIniFile(args)) {
				return;
			}

			if (Atol_kassa_info.FoundIniFile(args)) {
				//уходим на работу с файлом настройками
				WorkWithIniFile(args);
				return;
			}

		}

		#region Вывод информации об утилите и помощи по использованию

		static bool FindHelpInArgs(string[] ar)
		{

			bool res = false;
			if ((Array.IndexOf(ar, "--help") >= 0) | (Array.IndexOf(ar, "/?") >= 0)) {
				Console.WriteLine("Usage 1: atol_kassa_info /ini|--ini-file=<filename>");
				Console.WriteLine("Usage 2: atol_kassa_info /cr|--create-ini-file");
				Console.WriteLine("Info about: atol_kassa_info {/i|--info}");
				Console.WriteLine("Help: atol_kassa_info {/? | --help}");

				Console.WriteLine("Commands:");

				Console.WriteLine("\t /ini or --ini-file=: all settings get from file. It must be xml file, see it's structure in example");
				Console.WriteLine();

				Console.WriteLine("\t /cr or--create-ini-file: Creates sample ini file in directory with exe file");
				Console.WriteLine();

				res = true;
			}

			return res;
		}

		static bool FindVersionInArgs(string[] ar)
		{
			bool res = false;
			if ((Array.IndexOf(ar, "/i") >= 0) | (Array.IndexOf(ar, "--info") >= 0)) {
				Console.WriteLine("Name: Getting stats info from cash terminal using Atol driver");
				Console.WriteLine("Description: Tool made for Sirius, getting info from cash terminal and saving it to further analysing");
				Console.WriteLine("Version: 1.0");
				Console.WriteLine("Date: 2026/02/25");
				Console.WriteLine("Author: alexpvs");
				res = true;
			}
			return res;
		}

		#endregion

		#region Работа с ini файликом

		static bool FindCreatyEmptyIniFile(string[] ar)
		{
			bool res = false;
			if ((Array.IndexOf(ar, "/cr") >= 0) | (Array.IndexOf(ar, "--create-ini-file") >= 0)) {

				var ini = new IniFile();
				
				//ini.PathToAtolDLL		= "C:\\Program Files\\Atol\\fptr10.dll";
				ini.TypeOfConnection	= ConnectionTypeCom; //USB
				ini.ComPortNumber 		= 3;
				
				ini.PathToPSCP 			= "B:\\PRG\\tmp\\atol\\pscp.exe";
				
				ini.FTP_Server 			= "88.147.147.82";
				ini.FTP_Server_Port 	= 8021;
				ini.FTP_User 			= "solarisdirect";
				ini.FTP_Password 		= "127001#SolDirect";
				ini.FTP_Directory		= "/Sirius_FN";
				ini.FTP_PassiveMode		= true;
				
				ini.LocalDirectory 		= "B:\\PRG\\tmp\\atol";
				ini.WarehouseName		= "2932:\"Сириус - А\" Петровск а/п 3, Московская 6";
				
				string path_empty_ini = Directory.GetCurrentDirectory() + "\\" + "sample_ini.xml";

				using (var writer = new StreamWriter(path_empty_ini, false, new UTF8Encoding(false)))
				{
					var serializer = new XmlSerializer(typeof(IniFile));
					serializer.Serialize(writer, ini);
				}

				Console.WriteLine("Записан образец файла с настройками: " + path_empty_ini);
				res = true;

			}
			return res;
		}
		
		static bool FoundIniFile(string[] ar) {
			bool res = false;
			foreach(string str in ar) {
				if ((str.StartsWith("/ini", StringComparison.CurrentCultureIgnoreCase)) | (str.StartsWith("--ini-file", StringComparison.CurrentCultureIgnoreCase))) {
					res = true;
					break;
				}

			}
			return res;

		}
		
		#endregion
		
		static string AddComment(string ПолнаяСтрока, string Коммент)
		{
			
			string res = ПолнаяСтрока;
			if (ПолнаяСтрока.Length == 0)
				res = Коммент;
			else
				res = res + "," + Коммент;
			
			return res;
		
		}
		
		static AtolData GetAtolInfo(IniFile iniSet)
		{
			
			IFptr fptr_lib = new Fptr();
			
			AtolData data;
			
			if (iniSet.TypeOfConnection == ConnectionTypeCom) {
				fptr_lib.setSingleSetting(Constants.LIBFPTR_SETTING_MODEL, Constants.LIBFPTR_MODEL_ATOL_AUTO.ToString());
				fptr_lib.setSingleSetting(Constants.LIBFPTR_SETTING_PORT, Constants.LIBFPTR_PORT_COM.ToString());
				fptr_lib.setSingleSetting(Constants.LIBFPTR_SETTING_COM_FILE, String.Format("COM{0:D}", iniSet.ComPortNumber));
				fptr_lib.setSingleSetting(Constants.LIBFPTR_SETTING_BAUDRATE, Constants.LIBFPTR_PORT_BR_115200.ToString());
				
			} else if (iniSet.TypeOfConnection == ConnectionTypeUsb) {
				
				fptr_lib.setSingleSetting(Constants.LIBFPTR_SETTING_PORT, Constants.LIBFPTR_PORT_USB.ToString());
			}
			
			fptr_lib.applySingleSettings();
			
			data.CurrStorage = iniSet.WarehouseName;
			data.HOSTNAME = Environment.MachineName;
			
			fptr_lib.open();
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);
			fptr_lib.queryData();
			
			uint НомерМодели     		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_MODEL);          //=67
			uint ТекущийНомерДокумента  = fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER);//=27902
			uint ШиринаЛенты      		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_RECEIPT_LINE_LENGTH);//=42
			DateTime ТекущаяДатаВремя	= fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);//= Текущее время
			string НомерККТ			    = fptr_lib.getParamString(Constants.LIBFPTR_PARAM_SERIAL_NUMBER);//=
			
			string ИмяМодели		        = fptr_lib.getParamString(Constants.LIBFPTR_PARAM_MODEL_NAME);//=
			string ВерсияККМ				= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_UNIT_VERSION);//=
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_PAYMENT_SUM);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_CASH);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, Constants.LIBFPTR_RT_SELL);
			fptr_lib.queryData();
			double СуммаПродажНал = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_PAYMENT_SUM);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_ELECTRONICALLY);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, Constants.LIBFPTR_RT_SELL);
			fptr_lib.queryData();
			double СуммаПродажБН = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_PAYMENT_SUM);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_CASH);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, Constants.LIBFPTR_RT_SELL_RETURN);
			fptr_lib.queryData();
			double СуммаВозвратовНал = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_PAYMENT_SUM);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_ELECTRONICALLY);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, Constants.LIBFPTR_RT_SELL_RETURN);
			fptr_lib.queryData();
			double СуммаВозвратовБН = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			
			const string ФДЧ = "{0:0.00}";
			
			data.SalesAmount 			= String.Format(ФДЧ, (СуммаПродажНал + СуммаПродажБН)); //Сумма продаж (чеки продажи)
			data.RefundAmount 			= String.Format(ФДЧ, (СуммаВозвратовНал + СуммаВозвратовБН)); //Сумма возвратов (чеки возврата продажи)
			data.CashSalesAmount 		= String.Format(ФДЧ, СуммаПродажНал); //Сумма продаж (наличными)
			data.CashRefundAmount 		= String.Format(ФДЧ, СуммаВозвратовНал); //Сумма возвратов (наличными)
			data.CashlessSalesAmount 	= String.Format(ФДЧ, СуммаПродажБН); //Сумма продаж (безнал)
			data.CashlessRefundAmount 	= String.Format(ФДЧ, СуммаВозвратовБН); //Сумма возвратов (безнал)
			
			data.CreditSalesAmount 			= String.Format(ФДЧ, 0); //Сумма продаж (кредит)
			data.CreditRefundAmount 		= String.Format(ФДЧ, 0); //Сумма возвратов (кредит)
			data.CancelledReceiptsAmount 	= "Не поддерживается в данной версии"; //Сумма отмененных чеков
			data.CancelledRefundsAmount 	= "Не поддерживается в данной версии"; //Сумма отмененных возвратов
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_CASHIN_SUM);
			fptr_lib.queryData();
			double СуммаВнесений = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			data.DepositAmount = String.Format(ФДЧ, СуммаВнесений); //Сумма внесений
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_CASHOUT_SUM);
			fptr_lib.queryData();
			double СуммаВыплат = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			data.PayoutAmount = String.Format(ФДЧ, СуммаВыплат); //Сумма выплат
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_CASH_SUM);
			fptr_lib.queryData();
			double СуммаНаличности = fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_SUM);
			data.CashAmount = String.Format(ФДЧ, СуммаНаличности); //Сумма наличности
			
			data.RevenueAmount = "Не поддерживается в данной версии"; //Сумма выручки

			data.CurrentDateTime = String.Format("{0:G}", ТекущаяДатаВремя); //Текущие дата и время
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_SHIFT_STATE);
			fptr_lib.queryData();
			uint КодСостоянияСмены			= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_SHIFT_STATE);
			uint НомерТекущейСмены			= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_SHIFT_NUMBER);
			DateTime ДатаОкончанияСмены		= fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
			
			string СостояниеСмены;
			if (КодСостоянияСмены == Constants.LIBFPTR_SS_OPENED) {
				СостояниеСмены = "Смена открыта";
			} else if (КодСостоянияСмены == Constants.LIBFPTR_SS_EXPIRED) {
				СостояниеСмены = "Смена истекла";
			} else {
				СостояниеСмены = "Смена закрыта";
			}
			
			data.ShiftStatus 				= СостояниеСмены; //Состояние смены
			data.ShiftNumber 				= String.Format("{0:D}", НомерТекущейСмены); //Номер смены
			data.ShiftExpirationTime 		= String.Format("{0:G}", ДатаОкончанияСмены); //Время истечения текущей смены
			data.CashRegisterSerialNumber 	= НомерККТ; //Заводской номер ККМ
			data.CurrentSessionNumber 		= String.Format("{0:D}", НомерТекущейСмены); //Текущий номер сессии
			data.CurrentDocumentNumber 		= String.Format("{0:D}", ТекущийНомерДокумента); //Текущий номер документа
			data.CashRegisterModelNumber 	= String.Format("{0:D}", НомерМодели); //Номер модели ККМ
			data.CashRegisterName 			= ИмяМодели; //Наименование ККМ
			data.CashRegisterVersion 		= ВерсияККМ; //Версия ККМ
			data.DriverVersion 				= fptr_lib.version(); //Версия драйвера
			
			//Запрос версии прошивки
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_UNIT_VERSION);
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_UNIT_TYPE, Constants.LIBFPTR_UT_CONFIGURATION);
			fptr_lib.queryData();
			string ВерсияПрошивки = fptr_lib.getParamString(Constants.LIBFPTR_PARAM_UNIT_VERSION);
			
			data.FirmwareVersion = ВерсияПрошивки; //Версия прошивки
			data.ReleaseVersion = ""; //Версия релиза
			data.ReceiptWidth = String.Format("{0:D}", ШиринаЛенты); //Ширина ленты (симв.)
			
			//Запрос статуса информационного обмена с ОФД
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_OFD_EXCHANGE_STATUS);
			fptr_lib.fnQueryData();
			uint СтатусОбмена      		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_OFD_EXCHANGE_STATUS);
			uint Неотправлено         	= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENTS_COUNT);
			DateTime ДатаНеотправлено   = fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
			DateTime ДатаОбмена         = fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_LAST_SUCCESSFUL_OKP);
			
			data.OfdExchangeStatus = String.Format("{0:D}", СтатусОбмена); //Статус обмена с ОФД
			data.endingDocumentsCount = String.Format("{0:D}", Неотправлено); //Кол-во неотправленных документов в ОФД
			data.FirstPendingDocumentDateTime = String.Format("{0:G}", ДатаНеотправлено); //Дата и время первого не отправленного документа в ОФД
			data.LastSuccessfulOfdExchangeDateTime = String.Format("{0:G}", ДатаОбмена); //Дата и время последнего успешного обмена с ОФД
			
			/*
 			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_ISM_EXCHANGE_STATUS);
			fptr_lib.fnQueryData();

			data.IsmExchangeStatus = String.Format(ФДЧ, 0); //Статус информационного обмена с ИСМ
			data.PendingNotificationsCount = String.Format(ФДЧ, 0); //Кол-во непереданных уведомлений в ИСМ
			data.FirstPendingNotificationDateTime = String.Format(ФДЧ, 0); //Дата и время первого непереданного уведомления в ИСМ
			*/
			data.IsmExchangeStatus = "";
			data.PendingNotificationsCount = String.Format("{0:D}", 0);
			data.FirstPendingNotificationDateTime = "01.01.1970";
			
			//Запрос информации и статуса ФН
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_FN_INFO);
			fptr_lib.fnQueryData();
			string СерийныйНомерФН		= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_SERIAL_NUMBER);
			string ВерсияФН				= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_FN_VERSION);
			uint КодСостоянияФН 		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_FN_STATE);
			
			string СостояниеФН;
			
			if (КодСостоянияФН == Constants.LIBFPTR_FNS_INITIAL)
				СостояниеФН = "настройка ФН";
			else if (КодСостоянияФН == Constants.LIBFPTR_FNS_CONFIGURED)
				СостояниеФН = "готовность к активации";
			else if (КодСостоянияФН == Constants.LIBFPTR_FNS_FISCAL_MODE)
				СостояниеФН = "фискальный режим";
			else if (КодСостоянияФН == Constants.LIBFPTR_FNS_POSTFISCAL_MODE)
				СостояниеФН = "постфискальный режим";
			else
				СостояниеФН = "доступ к архиву";
			
			uint КодТипаФН			= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_FN_TYPE);
			string ТипФН;
			if (КодТипаФН == Constants.LIBFPTR_FNT_DEBUG)
				ТипФН = "отладочная версия";
			else if (КодТипаФН == Constants.LIBFPTR_FNT_RELEASE)
				ТипФН = "боевая версия";
			else
				ТипФН = "неизвестная (не удалось получить)";
			
			string СтрокаЗамечаний;
			СтрокаЗамечаний = "";
			if (fptr_lib.getParamBool(Constants.LIBFPTR_PARAM_FN_NEED_REPLACEMENT) == true)
				СтрокаЗамечаний = AddComment(СтрокаЗамечаний, "Нужна замена");
			
			if (fptr_lib.getParamBool(Constants.LIBFPTR_PARAM_FN_RESOURCE_EXHAUSTED) == true)
				СтрокаЗамечаний = AddComment(СтрокаЗамечаний, "Исчерпан ресурс");
			
			if (fptr_lib.getParamBool(Constants.LIBFPTR_PARAM_FN_MEMORY_OVERFLOW) == true)
				СтрокаЗамечаний = AddComment(СтрокаЗамечаний, "Память переполнена");
			
			if (fptr_lib.getParamBool(Constants.LIBFPTR_PARAM_FN_OFD_TIMEOUT) == true)
				СтрокаЗамечаний = AddComment(СтрокаЗамечаний, "Превышено время ожидания ответа");
			
			if (fptr_lib.getParamBool(Constants.LIBFPTR_PARAM_FN_CRITICAL_ERROR) == true)
				СтрокаЗамечаний = AddComment(СтрокаЗамечаний, "Критическая ошибка");
			
			//Регистрационные данные
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_REG_INFO);
			fptr_lib.fnQueryData();
			
			uint КодВерсииОФД		= fptr_lib.getParamInt(1209);
			string ВерсияФФД;
			
			if (КодВерсииОФД == Constants.LIBFPTR_FFD_1_0_5)
				ВерсияФФД = "105";
			else if (КодВерсииОФД == Constants.LIBFPTR_FFD_1_1)
				ВерсияФФД = "110";
			else if (КодВерсииОФД == Constants.LIBFPTR_FFD_1_2)
				ВерсияФФД = "120";
			else
				ВерсияФФД = "неизвестная";
			
			string АдресРасчетов		= fptr_lib.getParamString(1009);
			string ИННОрганизации		= fptr_lib.getParamString(1018);
			string ИмяОрганизации		= fptr_lib.getParamString(1048);
			string EmailОрганизации		= fptr_lib.getParamString(1117);
			string РегНомерККТ			= fptr_lib.getParamString(1037);
			string ИННОФД				= fptr_lib.getParamString(1017);
			string НазваниеОФД			= fptr_lib.getParamString(1046);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_ERRORS);
			fptr_lib.fnQueryData();
			uint КодОшибкиСети		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_NETWORK_ERROR);
			string ТекстОшибкиСети	= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_NETWORK_ERROR_TEXT);
			uint КодОшибкиОФД		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_OFD_ERROR);
			string ТекстОшибкиОФД	= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_OFD_ERROR_TEXT);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_FFD_VERSIONS);
			fptr_lib.fnQueryData();
			uint ВерсияФФДККТ    = fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_DEVICE_FFD_VERSION);
			
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_VALIDITY);
			fptr_lib.fnQueryData();
			DateTime СрокДействияФН = fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
			
			data.FiscalStorageExpirationDate 	= String.Format("{0:G}", СрокДействияФН); //Срок действия ФН
			data.FiscalStorageSerialNumber 		= СерийныйНомерФН; //Серийный номер ФН
			data.FiscalStorageVersion 			= ВерсияФН; //Версия ФН
			data.FiscalStorageType 				= ТипФН; //Тип ФН
			data.FiscalStorageStatus 			= СостояниеФН; //Состояние ФН
			data.FiscalStorageNotes 			= СтрокаЗамечаний; //Замечания по ФН
			data.Ffd_KKT_Version 				= String.Format("{0:D}", ВерсияФФДККТ); //Версия ФФД ККТ
			data.NetworkError 					= String.Format("{0:D}:{1}", КодОшибкиСети, ТекстОшибкиСети); //Ошибка сети
			data.OfdErrorMessage 				= String.Format("{0:D}:{1}", КодОшибкиОФД, ТекстОшибкиОФД); //Текст ошибки ОФД
			data.OrganizationInn 				= ИННОрганизации; //ИНН организации
			data.OrganizationName 				= ИмяОрганизации; //Название организации
			data.OrganizationEmail 				= EmailОрганизации; //EMAIL организации
			data.SettlementAddress 				= АдресРасчетов; //Адрес места расчетов
			data.DeviceRegistrationNumber 		= РегНомерККТ; //Регистрационный номер устройства
			
			data.OfdInn 		= ИННОФД; //ИНН ОФД
			data.OfdName 		= НазваниеОФД; //Название ОФД
			data.FfdVersion 	= ВерсияФФД; //Версия ФФД
			
			//Запрос информации о последнем чеке
			fptr_lib.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_LAST_RECEIPT);
			fptr_lib.fnQueryData();
			uint ПДФННомер 		= fptr_lib.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER);
			double ПДФНСумма 	= fptr_lib.getParamDouble(Constants.LIBFPTR_PARAM_RECEIPT_SUM);
			string ПДФНПризнак 	= fptr_lib.getParamString(Constants.LIBFPTR_PARAM_FISCAL_SIGN);
			DateTime ПДФНДата 	= fptr_lib.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
			
			data.LastFiscalDocumentNumber 		= String.Format("{0:D}", ПДФННомер); //Последний документ в ФН: номер
			data.LastFiscalDocumentDateTime 	= String.Format("{0:G}", ПДФНДата); //Последний документ в ФН: дата-время
			data.LastFiscalDocumentAmount 		= String.Format(ФДЧ, ПДФНСумма); //Последний документ в ФН: сумма
			data.LastFiscalDocumentFiscalSign 	= ПДФНПризнак; //Последний документ в ФН: фискальный признак
			
			string ПроверкаКМ = fptr_lib.getSingleSetting(Constants.LIBFPTR_SETTING_VALIDATE_MARK_WITH_FNM_ONLY);
			
			data.CashRegisterSelfTest 			= ПроверкаКМ; //Проверка КМ средствами драйвера

			fptr_lib.close();
			fptr_lib.destroy();
			
			return data;
			
		}
		
		static void WorkWithIniFile(string[] ar)
		{
		
			int IndexOfINIarg = -1;
			int variant = 0;
			
			foreach (string str in ar) {
				if (str.StartsWith("/ini", StringComparison.CurrentCultureIgnoreCase)) {
					variant = 1;
					IndexOfINIarg = Array.IndexOf(ar, str);
					break;
				}
				if (str.StartsWith("--ini-file=", StringComparison.CurrentCultureIgnoreCase)) {
					variant = 2;
					IndexOfINIarg = Array.IndexOf(ar, str);
					break;
				}
			}
			if (IndexOfINIarg == -1) {
				//как сюда попали?
				Console.WriteLine("Ошибка при обработке параметра командной строки, не нашли ini-file");
				return;
			}

			string strINIarg = ar[IndexOfINIarg];

			if (variant == 1) {
				strINIarg = strINIarg.Replace("/ini", "");
			}
			if (variant == 2) {
				strINIarg = strINIarg.Replace("--ini-file=", "");
			}

			string fullINIname = "";
			if (strINIarg.IndexOf("\\", StringComparison.CurrentCultureIgnoreCase) == -1) {
				fullINIname = @"" + Directory.GetCurrentDirectory() + "\\" + strINIarg;
			} else {
				fullINIname = strINIarg;
			}

			if (fullINIname.Length == 0)
				return;

			IniFile iniSet;

			using (var reader = new StreamReader(fullINIname, new UTF8Encoding(false)))
			{
				var serializer = new XmlSerializer(typeof(IniFile));
				iniSet = (IniFile)serializer.Deserialize(reader);
			}
			
			//получим данные с кассы
			AtolData DataStruct = GetAtolInfo(iniSet);
			
			//сохраним структуру в текст в локальный каталог
			string local_file_name = SaveDataToLocalDir(DataStruct, iniSet);
			
			//отправим его на ftp через pscp
			SendFileToFTP(local_file_name, iniSet);
			
		}
		
		static string SaveDataToLocalDir(AtolData DataStruct, IniFile iniSet)
		{
			string local_file_name;
			if (iniSet.LocalDirectory.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase) == true) {
				local_file_name = iniSet.LocalDirectory + "new_fn_1234.txt";
			} else {
				local_file_name = iniSet.LocalDirectory + "\\new_fn_1234.txt";
			}
			
			const string delim = ":";
			using (var wr = new StreamWriter(local_file_name, false, new UTF8Encoding(false))) {
				wr.WriteLine("CurrStorage" + delim + DataStruct.CurrStorage);
				wr.WriteLine("HOSTNAME" + delim + DataStruct.HOSTNAME);
				
				wr.WriteLine("Сумма продаж (чеки продажи)" + delim + DataStruct.SalesAmount);
				wr.WriteLine("Сумма возвратов (чеки возврата продажи)" + delim + DataStruct.RefundAmount);
				wr.WriteLine("Сумма продаж (наличными)" + delim + DataStruct.CashSalesAmount);
				wr.WriteLine("Сумма возвратов (наличными)" + delim + DataStruct.CashRefundAmount);
				
				wr.WriteLine("Сумма продаж (безнал)" + delim + DataStruct.CashlessSalesAmount);
				wr.WriteLine("Сумма возвратов (безнал)" + delim + DataStruct.CashlessRefundAmount);
				wr.WriteLine("Сумма продаж (кредит)" + delim + DataStruct.CreditSalesAmount);
				wr.WriteLine("Сумма возвратов (кредит)" + delim + DataStruct.CreditRefundAmount);
				wr.WriteLine("Сумма отмененных чеков" + delim + DataStruct.CancelledReceiptsAmount);
				wr.WriteLine("Сумма отмененных возвратов" + delim + DataStruct.CancelledRefundsAmount);
				wr.WriteLine("Сумма внесений" + delim + DataStruct.DepositAmount);
				wr.WriteLine("Сумма выплат" + delim + DataStruct.PayoutAmount);
				wr.WriteLine("Сумма наличности" + delim + DataStruct.CashAmount);
				wr.WriteLine("Сумма выручки" + delim + DataStruct.RevenueAmount);
				
				wr.WriteLine("Текущие дата и время" + delim + DataStruct.CurrentDateTime);
				wr.WriteLine("Состояние смены" + delim + DataStruct.ShiftStatus);
				wr.WriteLine("Номер смены" + delim + DataStruct.ShiftNumber);
				wr.WriteLine("Время истечения текущей смены" + delim + DataStruct.ShiftExpirationTime);
				wr.WriteLine("Заводской номер ККМ" + delim + DataStruct.CashRegisterSerialNumber);
				wr.WriteLine("Текущий номер сессии" + delim + DataStruct.CurrentSessionNumber);
				wr.WriteLine("Текущий номер документа" + delim + DataStruct.CurrentDocumentNumber);
				wr.WriteLine("Номер модели ККМ" + delim + DataStruct.CashRegisterModelNumber);
				wr.WriteLine("Наименование ККМ" + delim + DataStruct.CashRegisterName);
				wr.WriteLine("Версия ККМ" + delim + DataStruct.CashRegisterVersion);
				wr.WriteLine("Версия драйвера" + delim + DataStruct.DriverVersion);
				
				wr.WriteLine("Версия прошивки" + delim + DataStruct.FirmwareVersion);
				wr.WriteLine("Версия релиза" + delim + DataStruct.ReleaseVersion);
				wr.WriteLine("Ширина ленты (симв.)" + delim + DataStruct.ReceiptWidth);
				
				wr.WriteLine("Статус обмена с ОФД" + delim + DataStruct.OfdExchangeStatus);
				wr.WriteLine("Кол-во неотправленных документов в ОФД" + delim + DataStruct.endingDocumentsCount);
				wr.WriteLine("Дата и время первого не отправленного документа в ОФД" + delim + DataStruct.FirstPendingDocumentDateTime);
				wr.WriteLine("Дата и время последнего успешного обмена с ОФД" + delim + DataStruct.LastSuccessfulOfdExchangeDateTime);
				wr.WriteLine("Статус информационного обмена с ИСМ" + delim + DataStruct.IsmExchangeStatus);
				wr.WriteLine("Кол-во непереданных уведомлений в ИСМ" + delim + DataStruct.PendingNotificationsCount);
				wr.WriteLine("Дата и время первого непереданного уведомления в ИСМ" + delim + DataStruct.FirstPendingNotificationDateTime);
				wr.WriteLine("Срок действия ФН" + delim + DataStruct.FiscalStorageExpirationDate);
				wr.WriteLine("Серийный номер ФН" + delim + DataStruct.FiscalStorageSerialNumber);
				wr.WriteLine("Версия ФН" + delim + DataStruct.FiscalStorageVersion);
				wr.WriteLine("Тип ФН" + delim + DataStruct.FiscalStorageType);
				wr.WriteLine("Состояние ФН" + delim + DataStruct.FiscalStorageStatus);
				wr.WriteLine("Замечания по ФН" + delim + DataStruct.FiscalStorageNotes);
				wr.WriteLine("Версия ФФД ККТ" + delim + DataStruct.Ffd_KKT_Version);
				wr.WriteLine("Ошибка сети" + delim + DataStruct.NetworkError);
				wr.WriteLine("Текст ошибки ОФД" + delim + DataStruct.OfdErrorMessage);
				wr.WriteLine("ИНН организации" + delim + DataStruct.OrganizationInn);
				wr.WriteLine("Название организации" + delim + DataStruct.OrganizationName);
				wr.WriteLine("EMAIL организации" + delim + DataStruct.OrganizationEmail);
				wr.WriteLine("Адрес места расчетов" + delim + DataStruct.SettlementAddress);
				wr.WriteLine("Регистрационный номер устройства" + delim + DataStruct.DeviceRegistrationNumber);
				wr.WriteLine("ИНН ОФД" + delim + DataStruct.OfdInn);
				wr.WriteLine("Название ОФД" + delim + DataStruct.OfdName);
				wr.WriteLine("Версия ФФД" + delim + DataStruct.FfdVersion);
				wr.WriteLine("Последний документ в ФН: номер" + delim + DataStruct.LastFiscalDocumentNumber);
				wr.WriteLine("Последний документ в ФН: дата-время" + delim + DataStruct.LastFiscalDocumentDateTime);
				wr.WriteLine("Последний документ в ФН: сумма" + delim + DataStruct.LastFiscalDocumentAmount);
				wr.WriteLine("Последний документ в ФН: фискальный признак" + delim + DataStruct.LastFiscalDocumentFiscalSign);
				wr.WriteLine("Проверка КМ средствами драйвера" + delim + DataStruct.CashRegisterSelfTest);
			}
			
			return local_file_name;
		}
		
		static void SendFileToFTP(string local_file_name, IniFile iniSet)
		{
		}
		
		
	}

	#region Объявления структур данных
	
	public struct IniFile
	{
		//public string PathToAtolDLL;
		public string TypeOfConnection;
		public int ComPortNumber;
		
		public string PathToPSCP;
		
		public string FTP_Server;
		public int FTP_Server_Port;
		public string FTP_User;
		public string FTP_Password;
		public string FTP_Directory;
		public bool FTP_PassiveMode;
		
		public string LocalDirectory;
		public string WarehouseName;
		
	}
	
	public struct AtolData
	{
		
		public string CurrStorage;
		public string HOSTNAME;
		
		public string SalesAmount; //Сумма продаж (чеки продажи)
		public string RefundAmount; //Сумма возвратов (чеки возврата продажи)
		public string CashSalesAmount; //Сумма продаж (наличными)
		public string CashRefundAmount; //Сумма возвратов (наличными)
		public string CashlessSalesAmount; //Сумма продаж (безнал)
		public string CashlessRefundAmount; //Сумма возвратов (безнал)
		public string CreditSalesAmount; //Сумма продаж (кредит)
		public string CreditRefundAmount; //Сумма возвратов (кредит)
		public string CancelledReceiptsAmount; //Сумма отмененных чеков
		public string CancelledRefundsAmount; //Сумма отмененных возвратов
		public string DepositAmount; //Сумма внесений
		public string PayoutAmount; //Сумма выплат
		public string CashAmount; //Сумма наличности
		public string RevenueAmount; //Сумма выручки
		
		public string CurrentDateTime; //Текущие дата и время
		public string ShiftStatus; //Состояние смены
		public string ShiftNumber; //Номер смены
		public string ShiftExpirationTime; //Время истечения текущей смены
		public string CashRegisterSerialNumber; //Заводской номер ККМ
		public string CurrentSessionNumber; //Текущий номер сессии
		public string CurrentDocumentNumber; //Текущий номер документа
		public string CashRegisterModelNumber; //Номер модели ККМ
		public string CashRegisterName; //Наименование ККМ
		public string CashRegisterVersion; //Версия ККМ
		public string DriverVersion; //Версия драйвера
		public string FirmwareVersion; //Версия прошивки
		public string ReleaseVersion; //Версия релиза
		public string ReceiptWidth; //Ширина ленты (симв.)
		
		public string OfdExchangeStatus; //Статус обмена с ОФД
		public string endingDocumentsCount; //Кол-во неотправленных документов в ОФД
		public string FirstPendingDocumentDateTime; //Дата и время первого не отправленного документа в ОФД
		public string LastSuccessfulOfdExchangeDateTime; //Дата и время последнего успешного обмена с ОФД
		public string IsmExchangeStatus; //Статус информационного обмена с ИСМ
		public string PendingNotificationsCount; //Кол-во непереданных уведомлений в ИСМ
		public string FirstPendingNotificationDateTime; //Дата и время первого непереданного уведомления в ИСМ
		
		public string FiscalStorageExpirationDate; //Срок действия ФН
		public string FiscalStorageSerialNumber; //Серийный номер ФН
		public string FiscalStorageVersion; //Версия ФН
		public string FiscalStorageType; //Тип ФН
		public string FiscalStorageStatus; //Состояние ФН
		public string FiscalStorageNotes; //Замечания по ФН
		public string Ffd_KKT_Version; //Версия ФФД ККТ
		public string NetworkError; //Ошибка сети
		public string OfdErrorMessage; //Текст ошибки ОФД
		public string OrganizationInn; //ИНН организации
		public string OrganizationName; //Название организации
		public string OrganizationEmail; //EMAIL организации
		public string SettlementAddress; //Адрес места расчетов
		public string DeviceRegistrationNumber; //Регистрационный номер устройства
		
		public string OfdInn; //ИНН ОФД
		public string OfdName; //Название ОФД
		public string FfdVersion; //Версия ФФД
		
		public string LastFiscalDocumentNumber; //Последний документ в ФН: номер
		public string LastFiscalDocumentDateTime; //Последний документ в ФН: дата-время
		public string LastFiscalDocumentAmount; //Последний документ в ФН: сумма
		public string LastFiscalDocumentFiscalSign; //Последний документ в ФН: фискальный признак
		public string CashRegisterSelfTest; //Проверка КМ средствами драйвера
		
	}
	
	#endregion

}