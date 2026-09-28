#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore

{
    public class MultipleKey
    {
        private string? ID { get; set; } = null;
        private string? IP { get; set; }= null;


        public MultipleKey(in string? id,in string? ip)
        {

        }

        public bool HasValidKey()
        {
            return ID!=null && IP!=null;
        }
        
    }
}

