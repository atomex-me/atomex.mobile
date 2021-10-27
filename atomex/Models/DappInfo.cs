using System;
using System.Collections.Generic;
using Atomex.Core;
using Microsoft.Extensions.FileProviders;

namespace atomex.ViewModel
{
    public class DappInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Network Network { get; set; }
        public bool IsActive { get; set; }

        private DappType _dappDeviceType;
        public string DappDeviceType
        {
            get => _dappDeviceType.ToString();
            set => _dappDeviceType = Enum.TryParse<DappType>(value, out var res) ? res : throw new KeyNotFoundException("Not found") ;
        }

        public IReadOnlyCollection<Permission> Permissions { get; set; }
    }

    public enum DappType
    {
        Mobile,
        Desktop,
        Web
    }

    public class Permission
    {
        public string Name { get; set; }
    }
}