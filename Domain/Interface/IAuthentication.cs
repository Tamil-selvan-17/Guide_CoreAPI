using Domain.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interface
{
    public interface IAuthentication:IDisposable
    {
        Task<(bool status, string message, string username, long userid)> Login(AuthenticationModel authentication);
        Task<(bool status, string message)> Register(AuthenticationModel register);
    }
}
