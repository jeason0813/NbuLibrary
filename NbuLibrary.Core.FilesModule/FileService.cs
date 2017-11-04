using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.FilesModule
{
    public class FileService : IFileService
    {
        private IEntityRepository _repository;
        private string _tempPath;
        private string _permPath;
        private int _bufferSize;
        private string[] _allowed;
        private ISecurityService _securityService;
        private IDatabaseService _dbService;


        public FileService(IEntityRepository repository, ISecurityService securityService, IDatabaseService dbService)
        {
            _repository = repository;
            _securityService = securityService;
            _dbService = dbService;

            FileServiceConfigurationSection config = ConfigurationManager.GetSection("fileService") as FileServiceConfigurationSection;
            _tempPath = config.TemporaryStoragePath;
            _permPath = config.PermanentStoragePath;
            _bufferSize = config.BufferSize;
            _allowed = new string[config.AllowedExtensions.Count];
            for (int i = 0; i < config.AllowedExtensions.Count; i++)
            {
                _allowed[i] = config.AllowedExtensions[i].Value;
            }
        }

        public Guid StoreFileContent(System.IO.Stream content)
        {
            Guid g = Guid.NewGuid();
            StoreFile(System.IO.Path.Combine(_permPath, g.ToString()), content);
            return g;
        }
        public void DeleteFileContent(Guid id)
        {
            var path = System.IO.Path.Combine(_permPath, id.ToString());
            try
            {
                System.IO.File.Delete(path);
            }
            catch (System.IO.IOException ioEx)
            {
                System.IO.File.Move(path, path);
                System.IO.File.Delete(path);
            }
        }


        public void SaveFile(Domain.File file)
        {
            //TODO: fileservice is using repository (bypassing operation logic)
            if (file.Id > 0)
                _repository.Update(file);
            else
                _repository.Create(file);
        }

        public System.IO.Stream GetFileContent(int fileId, Guid? token = null)
        {
            var q = new EntityQuery2(File.ENTITY, fileId);
            q.AddProperties("ContentPath");
            q.Include(User.ENTITY, Roles.Access);
            var file = new File(_repository.Read(q));
            if (HasAccessInternal(_securityService.CurrentUser, file.Access, token))
            {
                return new System.IO.FileStream(System.IO.Path.Combine(_permPath, file.ContentPath), System.IO.FileMode.Open);
            }
            else
                throw new UnauthorizedAccessException("You don't have permissions to access this file.");
        }

        public Domain.File GetFile(int fileId)
        {
            var q = new EntityQuery2(File.ENTITY, fileId);
            q.AllProperties = true;
            return new File(_repository.Read(q));
        }

        public bool HasAccess(Domain.User user, int fileId, Guid? token = null)
        {
            if (user.UserType == UserTypes.Admin)
                return true;

            var q = new EntityQuery2(File.ENTITY, fileId);
            q.Include(User.ENTITY, Roles.Access);
            var file = new File(_repository.Read(q));
            return HasAccessInternal(user, file.Access, token);
        }

        public bool HasAccess(User user, int fileId, FileAccessType accessType, Guid? token = null)
        {
            if (user.UserType == UserTypes.Admin)
                return true;
            else if (_securityService.HasModulePermission(user, FilesModule.Id, Permissions.ManageAll))
                return true;

            var q = new EntityQuery2(File.ENTITY, fileId);
            q.Include(User.ENTITY, Roles.Access);
            var relQuery = new RelationQuery(User.ENTITY, Roles.Access, user.Id);
            relQuery.RelationRules.Add(new Condition("Type", Condition.Is, accessType));
            q.WhereRelated(relQuery);

            var e = _repository.Read(q);
            if (e == null)
                return false;

            var file = new File(e);
            if (file.Access == null)
                return false;

            return HasAccessInternal(user, file.Access, token);
        }

        private void StoreFile(string fullpath, System.IO.Stream content)
        {
            using (var fs = System.IO.File.Create(fullpath))
            {
                byte[] buffer = new byte[_bufferSize];
                int bytesRead = content.Read(buffer, 0, buffer.Length);
                while (bytesRead > 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                    bytesRead = content.Read(buffer, 0, buffer.Length);
                }

                fs.Flush();
            }
        }

        private bool HasAccessInternal(User user, IEnumerable<NbuLibrary.Core.Domain.FileAccess> fileAccesses, Guid? token)
        {
            if (user.UserType == UserTypes.Admin)
                return true;
            else if (_securityService.HasModulePermission(user, FilesModule.Id, Permissions.ManageAll))
                return true;

            foreach (var fa in fileAccesses)
            {
                if (fa.User.Id != user.Id)
                    continue;

                if (fa.Type == FileAccessType.Owner || fa.Type == FileAccessType.Full || fa.Type == FileAccessType.Read)
                    return true;
                else if (fa.Type == FileAccessType.Temporary && fa.Expire.HasValue && fa.Expire.Value > DateTime.Now)
                    return true;
                else if (fa.Type == FileAccessType.Token
                    && token.HasValue
                    && fa.Token.HasValue
                    && token.Value == fa.Token.Value)
                {
                    if (fa.Expire.HasValue)
                        return fa.Expire > DateTime.Now;
                    else
                        return true;
                }
            }

            return false;
        }
        private bool HasAccessInternal(User user, IEnumerable<NbuLibrary.Core.Domain.FileAccess> fileAccesses, FileAccessType accessType, Guid? token)
        {
            if (fileAccesses == null)
                return false;

            foreach (var a in fileAccesses)
            {
                if (a.Type != accessType)
                    continue;
                else if (accessType == FileAccessType.Token
                    && a.Token.HasValue
                    && token.HasValue
                    && token.Value == a.Token.Value
                    && (!a.Expire.HasValue || a.Expire.Value > DateTime.Now))
                {
                    return true;
                }
                else if (accessType == FileAccessType.Temporary
                    && (!a.Expire.HasValue || a.Expire.Value > DateTime.Now))
                {
                    return true;
                }
                else if (accessType == FileAccessType.Owner || accessType == FileAccessType.Full || accessType == FileAccessType.Read)
                    return true;

            }
            return false;
        }


        public void GrantAccess(int fileId, FileAccessType accessType, User toUser, DateTime? expires = null, Guid? token = null)
        {
            var access = new FileAccess()
            {
                Type = accessType,
                User = toUser
            };
            if (expires.HasValue)
                access.Expire = expires.Value;
            if (token.HasValue)
                access.Token = token.Value;

            var q = new EntityQuery2(File.ENTITY, fileId);
            q.Include(User.ENTITY, Roles.Access);
            var file = new File(_repository.Read(q));

            if (_securityService.CurrentUser.UserType == UserTypes.Admin || HasAccessInternal(_securityService.CurrentUser, file.Access, FileAccessType.Owner, null) || HasAccessInternal(_securityService.CurrentUser, file.Access, FileAccessType.Full, null))
            {
                if (!HasAccessInternal(toUser, file.Access, token)) //TODO: FileService - upgrade access
                    _repository.Attach(file, access);
            }
            else
                throw new UnauthorizedAccessException("You don't have permissions to grant/deny permissions on that file.");//TODO: UnauthorizedAccessException
        }


        public CanUploadStatus CanUpload(string filename, int size)
        {
            bool extAllowed = FileExtensionAllowed(filename);
            if (!extAllowed)
                return CanUploadStatus.FileTypeNotAllowed;
            else
            {
                int uploaded = 0;
                using (var dbContext = _dbService.GetDatabaseContext(false))
                {
                    SqlCommand cmd = new SqlCommand(@"
select SUM(f.Size) 
from [File] f
inner join [File_User_Access] as fua ON fua.lid = f.Id
inner join [User] u on u.Id = fua.rid
where u.Id = @userId", dbContext.Connection);
                    cmd.Parameters.AddWithValue("userId", _securityService.CurrentUser.Id);
                    var raw = cmd.ExecuteScalar();
                    if (raw != DBNull.Value)
                        uploaded = (int)cmd.ExecuteScalar();
                    else
                        uploaded = 0;
                }

                if (uploaded > _securityService.CurrentUser.GetData<int>("DiskUsageLimit"))
                    return CanUploadStatus.DiskUsageLimitExceeded;
                else
                    return CanUploadStatus.Yes;
            }
        }

        private bool FileExtensionAllowed(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename);
            foreach (var allowed in _allowed)
            {
                if (ext.Equals(allowed, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
