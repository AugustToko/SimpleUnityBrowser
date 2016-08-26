﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageLibrary
{

    public enum DialogEventType
    {
        Alert = 0,
        Confirm = 1,
        Prompt = 2

    }

    [Serializable]
    public class DialogEvent : AbstractEvent
    {
        public DialogEventType Type;
        public string Message;
        public string DefaultPrompt;
        //reply
        public bool success;
        public string input;
    }
}
