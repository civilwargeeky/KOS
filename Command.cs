﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace kOS
{
    public class CommandAttribute : Attribute
    {
        public string[] Values { get; set; }
        public CommandAttribute(params string[] values) { this.Values = values; }

        public override String ToString()
        {
            return String.Join(",", Values);
        }
    }

    public static class CommandRegistry
    {
        public static Dictionary<String, Type> Bindings = new Dictionary<string, Type>();

        static CommandRegistry()
        {
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                CommandAttribute attr = (CommandAttribute)t.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault();
                if (attr != null)
                {
                    foreach (String s in attr.Values)
                    {
                        Bindings.Add(s.ToUpper(), t);
                    }
                }
            }
        }
    }

    public class Command : ExecutionContext
    {
        public float Time;
        public float WaitTime = 0;
        public String Input;
        public Match RegexMatch;
        public String InstanceName;
        public int LineNumber = -1;

        public Command(Match regexMatch, ExecutionContext context) : base(context)
        {
            this.RegexMatch = regexMatch;
        }

        public Command(String input, ExecutionContext context) : base(context)
        {
            this.Input = input;
        }

        public virtual void Evaluate()
        {
        }

        public static Command Get(String input, ExecutionContext context)
        {
            input = input.Trim().Replace("\n", " ");

            foreach (var kvp in CommandRegistry.Bindings)
            {
                Match match = Regex.Match(input, kvp.Key, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var command = (Command)Activator.CreateInstance(kvp.Value, match, context);
                    return command;
                }
            }

            throw new kOSException("Syntax Error.");
        }

        public virtual void Refresh()
        {
            this.State = ExecutionState.NEW;
        }

        public override void Lock(Command command)
        {
            if (ParentContext != null) ParentContext.Lock(command);
        }

        public override void Lock(string name, Expression expression)
        {
            if (ParentContext != null) ParentContext.Lock(name, expression);
        }

        public override void Unlock(Command command)
        {
            if (ParentContext != null) ParentContext.Unlock(command);
        }

        public override void Unlock(string name)
        {
            if (ParentContext != null) ParentContext.Unlock(name);
        }
    }
}
