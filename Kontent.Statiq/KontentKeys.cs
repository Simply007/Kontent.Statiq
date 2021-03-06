﻿namespace Kontent.Statiq
{
    /// <summary>
    /// Keys for well-known Statiq document metadata for Kontent.
    /// </summary>
    public static class KontentKeys
    {
        /// <summary>
        /// Keys for well-known Statiq document metadata for Kontent system metadata.
        /// </summary>
        public static class System
        {
            /// <summary>
            /// The item name.
            /// </summary>
            public const string Name = "system.name";
            /// <summary>
            /// The item code name.
            /// </summary>
            public const string CodeName = "system.codename";
            /// <summary>
            /// The language for the item.
            /// </summary>
            public const string Language = "system.language";
            /// <summary>
            /// The unique id for the item.
            /// </summary>
            public const string Id = "system.id";
            /// <summary>
            /// The type code name of the content.
            /// </summary>
            public const string Type = "system.type";
            /// <summary>
            /// The last modified date. This is a <c>DateTime</c> value.
            /// </summary>
            public const string LastModified = "system.lastmodified";
        }
    }
}