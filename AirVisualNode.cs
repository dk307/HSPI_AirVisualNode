using NullGuard;
using System;
using System.Net;

namespace Hspi
{
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal sealed class AirVisualNode : IEquatable<AirVisualNode>
    {
        public AirVisualNode(string id, string name, IPAddress deviceIP,
                            string username, string password)
        {
            Name = name;
            Password = password;
            Username = username;
            DeviceIP = deviceIP;
            Id = id;
        }

        public string Id { get; }
        public string Name { get; }
        public IPAddress DeviceIP { get; }
        public string Username { get; }
        public string Password { get; }

        public bool Equals([AllowNull]AirVisualNode other)
        {
            if (this == other)
            {
                return true;
            }

            return Id == other.Id &&
                Name == other.Name &&
                Username == other.Username &&
                Password == other.Password &&
                DeviceIP == other.DeviceIP;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AirVisualNode);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}