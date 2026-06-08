using System.Runtime.InteropServices;
using TestAuthentication.models;

namespace TestAuthentication.Services
{
    public class WindowsAuthentication
    {
        #region dll import
        [DllImport("advapi32.dll")]
        public static extern bool LogonUser(string username, string domain, string password, int logType,int logpv, ref IntPtr intPtr);
        #endregion
        public login TestAuthentication { get; set; }
        public WindowsAuthentication(login testAuthentication)
        {
            TestAuthentication = testAuthentication; 
        }
        public bool Authenticate()
        {
            try
            {
                bool isauthenticated = false;
                IntPtr ip = IntPtr.Zero;
                isauthenticated = LogonUser(TestAuthentication.Username, TestAuthentication.Domain, TestAuthentication.Password, 2,0,ref ip );
                return isauthenticated;
            }
            catch
            {
                throw;
            }
        }
    }
}
