using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Domain;

namespace NbuLibrary.Core.Services
{
    public interface ISessionService
    {
        /// <summary>
        /// Stores an object in the current session with a Singleton policy. 
        /// </summary>
        /// <typeparam name="T">Type of the stored object</typeparam>
        /// <param name="data">Tha actual object to be stored.</param>
        void Store<T>(T data);
        void Store<T>(T data, string key);

        /// <summary>
        /// Retrieves an object, previously stored as Singleton(without a key).
        /// </summary>
        /// <typeparam name="T">Type of the stored object.</typeparam>
        /// <returns>The stored object or null, if the object is not found.</returns>
        T Retrieve<T>();

        /// <summary>
        /// Retrieves an object, previously stored with key.
        /// </summary>
        /// <typeparam name="T">Type of the stored object.</typeparam>
        /// <param name="key">Key, with which the object was stored.</param>
        /// <returns>The stored object or null, if the object is not found.</returns>
        T Retrieve<T>(string key);
    }
}
