﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    public class ImmediateMode : ExecutionContext
    {
        private int cursor = 0;
        private int baseLineY = 0;
        private static int CMD_BACKLOG = 20;
        private List<String> previousCommands = new List<String>();
        private int prevCmdIndex = -1;
        private String inputBuffer = "";
        private String commandBuffer = "";
        private int CursorX = 0;
        private int CursorY = 0;
        private new Queue<Command> Queue = new Queue<Command>();

        private new char[,] buffer = new char[COLUMNS, ROWS];

        public ImmediateMode(ExecutionContext parent) : base(parent) 
        {
            StdOut("kOS Operating System Build " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision);
            StdOut("KerboScript v0.4");
            StdOut("");
            StdOut("Proceed.");
        }

        public void Add(string cmdString)
        {
            commandBuffer += cmdString;
            string nextCmd;

            while (parseNext(ref commandBuffer, out nextCmd))
            {
                try
                {
                    Command cmd = Command.Get(nextCmd, this);
                    Queue.Enqueue(cmd);
                }
                catch (kOSException e)
                {
                    StdOut(e.Message);
                    Queue.Clear(); // HALT!!
                }
            }
        }

        public override int GetCursorX()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : CursorX;
        }

        public override int GetCursorY()
        {
            return ChildContext != null ? ChildContext.GetCursorY() : CursorY;
        }

        public override bool Type(char ch)
        {
            if (base.Type(ch)) return true;

            switch (ch)
            {
                case (char)8:
                    if (cursor > 0)
                    {
                        inputBuffer = inputBuffer.Remove(cursor - 1, 1);
                        cursor--;
                    }
                    break;

                case (char)13:
                    Enter();
                    break;

                default:
                    inputBuffer = inputBuffer.Insert(cursor, new String(ch, 1));
                    cursor++;
                    break;
            }

            UpdateCursorXY();
            return true;
        }

        public override char[,] GetBuffer()
        {
            var childBuffer = (ChildContext != null) ? ChildContext.GetBuffer() : null;

            return childBuffer != null ? childBuffer : buffer;
        }

        public void UpdateCursorXY()
        {
            CursorX = cursor % buffer.GetLength(0);
            CursorY = (cursor / buffer.GetLength(0)) + baseLineY;
        }

        public void ShiftUp()
        {
            for (int y = 0; y < buffer.GetLength(1); y++)
            {
                for (int x = 0; x < buffer.GetLength(0); x++)
                {
                    if (y + 1 < buffer.GetLength(1))
                    {
                        buffer[x, y] = buffer[x, y + 1];
                    }
                    else
                    {
                        buffer[x, y] = (char)0;
                    }
                }
            }

            for (int x = 0; x < buffer.GetLength(0); x++)
            {
                buffer[x, buffer.GetLength(1) - 1] = (char)0;
            }

            if (baseLineY > 0) baseLineY--;

            UpdateCursorXY();
        }

        public override void Put(string text, int x, int y)
        {
            foreach (char c in text)
            {
                buffer[x, y] = c;
                x++;

                if (x > buffer.GetLength(0)) break;
            }
        }

        public override void StdOut(string line)
        {
            int linesWritten = WriteLine(line);
            baseLineY += linesWritten;
            UpdateCursorXY();
        }

        public void ClearScreen()
        {
            baseLineY = 0;
            cursor = 0;
            UpdateCursorXY();
        }

        public int WriteLine(string line)
        {
            int lineCount = (line.Length / buffer.GetLength(0)) + 1;

            while (baseLineY + lineCount > buffer.GetLength(1))
            {
                ShiftUp();
            }

            for (int y = baseLineY; y < buffer.GetLength(1); y++)
            for (int x = 0; x < buffer.GetLength(0); x++)
            {
                buffer[x, y] = (char)0;
            }

            char[] inputChars = line.ToCharArray();

            int writeX = 0;
            int writeY = baseLineY;
            foreach (char c in inputChars)
            {
                buffer[writeX, writeY] = c;

                writeX++;
                if (writeX >= buffer.GetLength(0)) { writeX = 0; writeY++; }
            }

            return lineCount;
        }

        public override void Update(float time)
        {
            if (Queue.Count == 0)
            {
                WriteLine(inputBuffer);
            }
            else
            {
                if (ChildContext == null)
                {
                    if (Queue.Count > 0)
                    {
                        var currentCmd = Queue.Dequeue();
                        
                        try
                        {
                            Push(currentCmd);
                            currentCmd.Evaluate();
                        }
                        catch (kOSException e)
                        {
                            StdOut(e.Message);
                            Queue.Clear();          // Halt all pending instructions
                            ChildContext = null;    //
                        }
                    }
                }
            }

            base.Update(time);
        }

        private void Enter()
        {
            baseLineY += WriteLine(inputBuffer);

            while (baseLineY > buffer.GetLength(1) - 1) ShiftUp();

            
            previousCommands.Add(inputBuffer);
            if (previousCommands.Count > CMD_BACKLOG)
            {
                int overflow = previousCommands.Count - CMD_BACKLOG;
                previousCommands.RemoveRange(0, overflow);
            }

            prevCmdIndex = -1;

            Add(inputBuffer += "\n");

            inputBuffer = "";
            cursor = 0;

            UpdateCursorXY();
        }

        public override void SendMessage(SystemMessage message)
        {
            switch (message)
            {
                case SystemMessage.CLEARSCREEN:
                    ClearScreen();
                    break;

                default:
                    base.SendMessage(message);
                    break;
            }
        }

        internal void Edit(string fileName)
        {
            //Cpu.PushInterpreter(new InterpreterEdit(Cpu, fileName));
        }

        public void PreviousCommand(int direction)
        {
            inputBuffer = "";
            cursor = 0;

            prevCmdIndex += direction;
            if (prevCmdIndex <= -1)
            {
                inputBuffer = "";
                prevCmdIndex = -1;
                cursor = 0;
                UpdateCursorXY();
                return;
            }
            if (prevCmdIndex > previousCommands.Count-1)
            {
                prevCmdIndex = previousCommands.Count - 1;
            }
            
            inputBuffer = previousCommands[(previousCommands.Count-1) - prevCmdIndex];
            cursor = inputBuffer.Length;
            UpdateCursorXY();
        }

        public override bool SpecialKey(kOSKeys key)
        {
            if (base.SpecialKey(key)) return true;

            switch (key)
            {
                case kOSKeys.UP:
                    PreviousCommand(1);
                    return true;

                case kOSKeys.DOWN:
                    PreviousCommand(-1);
                    return true;
            }

            return false;
        }
    }
}
