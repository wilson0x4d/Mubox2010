//***********************************************************************
// Assembly         : Mubox
// Author           : Shaun Wilson
// Created          : 04-15-2009
//
// Last Modified By : Shaun Wilson
// Last Modified On : 04-30-2011
// Description      :
//
// Copyright        : (c) Shaun Wilson. All rights reserved.
//***********************************************************************
using System;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public abstract class InputBase
    {
        private readonly DateTime _createdTime = DateTime.Now;

        public DateTime CreatedTime { get { return _createdTime; } }

        [DataMember]
        public uint Time { get; set; }

        public bool Handled { get; set; }

        public override string ToString()
        {
            return "";
        }
    }
}