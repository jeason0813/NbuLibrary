using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Domain;
using System.IO;
using System.Configuration;

namespace NbuLibrary.Core.Services
{
    public class FileServiceConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("tempStoragePath", IsRequired = true)]
        public string TemporaryStoragePath
        {
            get
            {
                return (string)this["tempStoragePath"];
            }
            set
            {
                this["tempStoragePath"] = value;
            }
        }

        [ConfigurationProperty("permanentStoragePath", IsRequired = true)]
        public string PermanentStoragePath
        {
            get
            {
                return (string)this["permanentStoragePath"];
            }
            set
            {
                this["permanentStoragePath"] = value;
            }
        }

        [ConfigurationProperty("bufferSize", IsRequired = false, DefaultValue = 1024)]
        public int BufferSize
        {
            get
            {
                return (int)this["bufferSize"];
            }
            set
            {
                this["bufferSize"] = value;
            }
        }

        [ConfigurationProperty("allowed", IsRequired = true)]
        [ConfigurationCollection(typeof(AllowedExtensionElement), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
        public AllowedExtensionsCollection AllowedExtensions
        {
            get
            {
                return (AllowedExtensionsCollection)this["allowed"];
            }
        }
    }

    public class AllowedExtensionsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new AllowedExtensionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AllowedExtensionElement)element).Value;
        }

        public AllowedExtensionElement this[int index]
        {
            get
            {
                return (AllowedExtensionElement)base.BaseGet(index);
            }
        }
    }

    public class AllowedExtensionElement : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get
            {
                return (string)this["value"];
            }
            set
            {
                this["value"] = true;
            }
        }
    }


    public enum CanUploadStatus
    {
        Yes,
        FileTypeNotAllowed,
        DiskUsageLimitExceeded
    }
    public interface IFileService
    {
        /// <summary>
        /// Stores the file in a permanent storage.
        /// </summary>
        /// <param name="content">Stream content to store.</param>
        /// <returns></returns>
        Guid StoreFileContent(System.IO.Stream content);
        void DeleteFileContent(Guid id);

        void SaveFile(NbuLibrary.Core.Domain.File file);

        Stream GetFileContent(int fileId, Guid? token = null);
        NbuLibrary.Core.Domain.File GetFile(int fileId);

        /// <summary>
        /// Grants access to the specified user. The current user must have full or owner permissions on the file in order to do so.
        /// </summary>
        /// <param name="fileId">The id of the file.</param>
        /// <param name="accessType">The access type that will be granted.</param>
        /// <param name="toUser">The user to whom the access will be granted.</param>
        /// <param name="expires">Optional. When the access will expire.</param>
        /// <param name="token">Optional. In case of token based access, this is the token the user will have to provide in order to read the file.</param>
        void GrantAccess(int fileId, FileAccessType accessType, User toUser, DateTime? expires = null, Guid? token = null);

        /// <summary>
        /// Checks if the specified user has access to file in the system.
        /// </summary>
        /// <param name="user">The user to use for the check.</param>
        /// <param name="fileId">The id of the file.</param>
        /// <param name="token">Optional. If the user is trying to access the file using a token, this token will be used to check the access.</param>
        /// <returns>True if the user has access, false otherwise.</returns>
        bool HasAccess(Domain.User user, int fileId, Guid? token = null);

        /// <summary>
        /// Checks if the user has a specific access to file in the system.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="fileId">The id of the file.</param>
        /// <param name="token">Optional. Token used for token based access.</param>
        /// <param name="accessType">The access type to check for.</param>
        /// <returns></returns>
        bool HasAccess(Domain.User user, int fileId, FileAccessType accessType, Guid? token = null);

        CanUploadStatus CanUpload(string filename, int size);
    }
}
