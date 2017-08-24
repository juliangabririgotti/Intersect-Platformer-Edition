﻿using System.Text;
using Intersect.Models;

namespace Intersect.GameObjects
{
    public class TilesetBase : DatabaseObject<TilesetBase>
    {
        public TilesetBase(int id) : base(id)
        {
            Name = "";
        }

        public new string Name
        {
            get { return base.Name; }
            set { base.Name = value?.Trim().ToLower(); }
        }

        public override byte[] BinaryData => Encoding.UTF8.GetBytes(Name);

        public override void Load(byte[] packet) => Name = Encoding.UTF8.GetString(packet, 0, packet.Length);
    }
}