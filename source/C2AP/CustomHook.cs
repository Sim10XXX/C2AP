using Archipelago.Core.Util;
using Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace C2AP
{
    internal class CustomHook
    {
        public List<string> _asm;

        private List<byte> _bytes;

        private ulong _targetAddress;

        private int _targetInstructionSize;

        public ulong _freeAddress;

        public ulong _hookSize;

        private bool _isJumptablePatch;

        public CustomHook(List<string> asm) { 
            _asm = asm;
            _bytes = ConvertAsm(asm);
            //_bytes.Reverse();
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }

        public void ReplaceAsm(List<string> asm)
        {
            if (_targetAddress == 0 && _freeAddress == 0)
            {
                Log.Warning("can't run ReplaceAsm on uninserted hook");
                return;
            }
            ulong targetAddress = _targetAddress;
            int targetInstructionSize = _targetInstructionSize;
            ulong freeAddress = _freeAddress;
            
            RemoveHook();
            _asm = asm;
            _bytes = ConvertAsm(asm);
            //Log.Information("replaceasm1");
            InsertHook(targetAddress, freeAddress);
            //Log.Information("replaceasm2");
        }

        private static byte EncodeRegister(string register)
        {
            byte result = 0;
            switch (register[1])
            {
                case 'z':
                    return 0;
                case 'v':
                    result = 0x2;
                    break;
                case 'a':
                    result = 0x4;
                    break;
                case 't':
                    result = 0x8;
                    if (register[2] == '8' || register[2] == '9')
                    {
                        Log.Error("$t8 & $t9 are not encode-able yet");
                    }
                    break;
                case 's':
                    if (register[2] == 'p')
                    {
                        return 0x1D;
                    }
                    result = 0x10;
                    break;
                case 'r': // $ra
                    return 0x1F;


            }
            
            result += Convert.ToByte(register[2].ToString());

            return result;
        }
        public static string DecodeRegister(uint regNum)
        {
            return regNum switch
            {
                0 => "$zero",
                1 => "$at",
                2 or 3 => $"$v{regNum - 2}",
                4 or 5 or 6 or 7 => $"$a{regNum - 4}",
                8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 => $"$t{regNum - 8}",
                16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 => $"$s{regNum - 16}",
                29 => "$sp",
                31 => "$ra",
                _ => "$unknown",
            };
        }
        public static void DecodeITypeInstruction(uint instruction, out string opcode, out string rs, out string rt, out string immed)
        {
            uint opcodeNum;
            uint rsNum;
            uint rtNum;
            uint immedNum;
            opcodeNum = (instruction >> 26) & 0x3F;
            rsNum = (instruction >> 21) & 0x1F;
            rtNum = (instruction >> 16) & 0x1F;
            immedNum = (ushort)(instruction & 0xFFFF);
            opcode = opcodeNum switch
            {
                0x4 => "beq",
                0x5 => "bne",
                0x9 => "addiu",
                0x23 => "lw",
                _ => "unknown",
            };
            rs = DecodeRegister(rsNum);
            rt = DecodeRegister(rtNum);
            immed = $"0x{immedNum:X}";
        }
        private static byte[] ConvertIType(string[] instruction)
        {
            uint opcode;
            uint rs;
            uint rt;
            uint immed;
            switch (instruction[0])
            {
                case "bgez":
                    opcode = 0x1;
                    rt = 0x1;
                    rs = EncodeRegister(instruction[1]);
                    immed = Convert.ToUInt32(instruction[2].Replace("0x", ""), 16) & 0xFFFF;
                    return ConvertToBytes(opcode, rs, rt, immed);
            }

            if (instruction.Length != 4)
            {
                Log.Error($"CustomHook: Invalid {instruction[0]}, length was {instruction.Length}");
                return [0, 0, 0, 0];
            }
            switch (instruction[0])
            {
                case "beq":
                    opcode = 0x4;
                    break;
                case "bne":
                    opcode = 0x5;
                    break;
                case "addiu":
                    opcode = 0x9; //might need to flip rt and rs, but it doesn't matter right now since they are equal in our usage
                    break;
                case "andi":
                    opcode = 0xC;
                    break;
                //case "lw":
                //    opcode = 0x23;
                //    break;
                default:
                    Log.Error($"CustomHook: Unknown/unimplemented I-type instruction {instruction[0]}");
                    return [0, 0, 0, 0];
            }
            
            switch (instruction[0])
            {
                case "addiu":
                case "andi":
                    rs = EncodeRegister(instruction[2]);
                    rt = EncodeRegister(instruction[1]);
                    break;
                default:
                    rs = EncodeRegister(instruction[1]);
                    rt = EncodeRegister(instruction[2]);

                    //immed = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0xFFFF;
                    break;
            }
            immed = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0xFFFF;

            return ConvertToBytes(opcode, rs, rt, immed);
        }

        public static List<byte> ConvertAsm(List<string> asm)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < asm.Count; i++)
            {
                byte[] instructionBytes = new byte[4];
                asm[i] = asm[i].Replace(", ", " ");
                string[] instruction = asm[i].Split([' ', ',']);
                string[] tempsplit;
                uint address;
                ushort upper;
                uint opcode = 0;
                uint rs = 0;
                uint rt = 0;
                uint rd = 0;
                uint shamt = 0;
                uint funct = 0;
                uint immed = 0;
                //uint encoding = 0;
                //Log.Information($"CustomHook: Converting instruction at line {i + 1}: {asm[i]}");
                switch (instruction[0])
                {
                    case "jmp":
                        {
                            instructionBytes[0] = 0x08;
                            if (instruction.Length != 2)
                            {
                                Log.Error($"CustomHook: Invalid jmp instruction format at line {i + 1}");
                                break;
                            }
                            instruction[1] = instruction[1].Replace("0x", "");
                            address = Convert.ToUInt32(instruction[1], 16);
                            address >>= 2;
                            instructionBytes[0] = (byte)(instructionBytes[0] | ((address >> 24) & 0x03));
                            instructionBytes[1] = (byte)((address>>16) & 0xFF);
                            instructionBytes[2] = (byte)((address>>8) & 0xFF);
                            instructionBytes[3] = (byte)(address & 0xFF);
                            instructionBytes.Reverse();
                            bytes.AddRange(instructionBytes);
                            break;
                        }
                    case "jal":
                        {
                            instructionBytes[0] = 0x0C;
                            if (instruction.Length != 2)
                            {
                                Log.Error($"CustomHook: Invalid jal instruction format at line {i + 1}");
                                break;
                            }
                            instruction[1] = instruction[1].Replace("0x", "");
                            address = Convert.ToUInt32(instruction[1], 16);
                            address >>= 2;
                            instructionBytes[0] = (byte)(instructionBytes[0] | ((address >> 24) & 0x03));
                            instructionBytes[1] = (byte)((address >> 16) & 0xFF);
                            instructionBytes[2] = (byte)((address >> 8) & 0xFF);
                            instructionBytes[3] = (byte)(address & 0xFF);
                            instructionBytes.Reverse();
                            bytes.AddRange(instructionBytes);
                            break;
                        }
                    case "nop":
                        {
                            bytes.AddRange(instructionBytes);
                            break;
                        }
                    case "la":
                        {
                            if (instruction.Length != 3)
                            {
                                Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                                break;
                            }
                            //if (!instruction[1].StartsWith("$t"))
                            //{
                            //    Log.Error($"CustomHook: Invalid {instruction[0]} instruction register at line {i + 1} (only $t0 - $t7 are supported)");
                            //    break;
                            //}
                            opcode = 0xF; //lui

                            //byte regNum = Convert.ToByte(instruction[1].Replace("$t", ""));
                            rt = EncodeRegister(instruction[1]);
                            rs = 0;

                            instruction[2] = instruction[2].Replace("0x", "");
                            address = Convert.ToUInt32(instruction[2], 16);
                            upper = (ushort)(address >> 16);
                            if (upper != 0) 
                                upper++;

                            immed = upper;

                            bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));

                            opcode = 0x9; //addiu
                            //opcode = 0x0D; //ori
                            immed = address & 0xFFFF;
                            rs = rt;

                            bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));

                            break;
                            //instructionBytes[0] = 0x3C;
                            
                            
                            //instructionBytes[1] = (byte)(8 + regNum); // $t0 - $t7
                            
                            
                            //instructionBytes[2] = (byte)((upper >> 8) & 0xFF);
                            //instructionBytes[3] = (byte)((upper) & 0xFF);
                            //Log.Information($"instruction: {BitConverter.ToString(instructionBytes)}");
                            //bytes.AddRange(instructionBytes);
                            //instructionBytes = new byte[4];
                            //instructionBytes[0] = 0x25;
                            //instructionBytes[1] = (byte)((regNum << 5) | (regNum) | 8);
                            //instructionBytes[2] = (byte)((address >> 8) & 0xFF);
                            //instructionBytes[3] = (byte)(address & 0xFF);
                            //Log.Information($"instruction: {BitConverter.ToString(instructionBytes)}");
                            //break;
                        }
                    case "lui":
                        if (instruction.Length != 3)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0xF; //lui
                        rt = EncodeRegister(instruction[1]);
                        rs = 0;

                        instruction[2] = instruction[2].Replace("0x", "");
                        address = Convert.ToUInt32(instruction[2], 16);
                        upper = (ushort)(address);
                        //upper++;

                        immed = upper;

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;
                    case "lw":
                        if (instruction.Length != 3)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x23; //lw

                        //0($t0)
                        tempsplit = instruction[2].Replace(")", "").Split('(');

                        if (tempsplit.Length != 2)
                        {
                            Log.Error($"CustomHook: tempsplit didn't work as intended (length = {tempsplit.Length})");
                            break;
                        }

                        rs = EncodeRegister(tempsplit[1]);
                        rt = EncodeRegister(instruction[1]);

                        immed = Convert.ToUInt32(tempsplit[0].Replace("0x", ""), 16);

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;

                    case "sw":
                        if (instruction.Length != 3)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x2B; //sw

                        //0($t0)
                        tempsplit = instruction[2].Replace(")", "").Split('(');

                        if (tempsplit.Length != 2)
                        {
                            Log.Error($"CustomHook: tempsplit didn't work as intended (length = {tempsplit.Length})");
                            break;
                        }
                        
                        rs = EncodeRegister(tempsplit[1]);
                        rt = EncodeRegister(instruction[1]);

                        immed = Convert.ToUInt32(tempsplit[0].Replace("0x", ""), 16);

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;

                    case "ori":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0x0D; //ori


                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[1]);

                        immed = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0xFFFF;

                        bytes.AddRange(ConvertToBytes(opcode, rs, rt, immed));
                        break;
                    case "or":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x25; //or
                        rd = EncodeRegister(instruction[1]);
                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[3]);
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "subu":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x23; //subu
                        rd = EncodeRegister(instruction[1]);
                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[3]);
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "addu":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x21; //addu
                        rd = EncodeRegister(instruction[1]);
                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[3]);
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "and":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x24; //and
                        rd = EncodeRegister(instruction[1]);
                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[3]);
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "sll":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x00; //sll
                        rd = EncodeRegister(instruction[1]);
                        rt = EncodeRegister(instruction[2]);
                        shamt = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0x1F;
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "srl":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x02; //srl
                        rd = EncodeRegister(instruction[1]);
                        rt = EncodeRegister(instruction[2]);
                        shamt = Convert.ToUInt32(instruction[3].Replace("0x", ""), 16) & 0x1F;
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "nor":
                        if (instruction.Length != 4)
                        {
                            Log.Error($"CustomHook: Invalid {instruction[0]} instruction format at line {i + 1}, length was {instruction.Length}");
                            break;
                        }
                        opcode = 0; //r type
                        funct = 0x27; //nor
                        rd = EncodeRegister(instruction[1]);
                        rs = EncodeRegister(instruction[2]);
                        rt = EncodeRegister(instruction[3]);
                        bytes.AddRange(ConvertToBytes(rs, rt, rd, shamt, funct));
                        break;
                    case "beq":
                    case "bne":
                    case "bgez":
                    case "addiu":
                    case "andi":
                    //case "lw":
                        bytes.AddRange(ConvertIType(instruction));
                        break;
                    default:
                        {
                            Log.Error($"CustomHook: Unknown instruction {instruction[0]}");
                            break;
                        }
                }
                //bytes.AddRange(instructionBytes);
            }

            return bytes;
        }

        public void LogHookBytes()
        {
            if (_bytes.Count%4 != 0)
            {
                Log.Error("Not mult of 4 somehow like why");
            }
            for (int i = 0; i < _bytes.Count; i+=4)
            {
                
                Log.Information($"line {i}: {Convert.ToHexString([_bytes[i], _bytes[i+1], _bytes[i+2], _bytes[i+3]])}");
            }
        }

        private static byte[] ConvertToBytes(uint opcode, uint rs, uint rt, uint immed)
        {
            uint encoding = 0;
            encoding |= opcode << 26;
            encoding |= rs << 21;
            encoding |= rt << 16;
            encoding |= immed;
            byte[] bytes = BitConverter.GetBytes(encoding);
            //bytes.Reverse();
            return bytes;
        }

        private static byte[] ConvertToBytes(uint rs, uint rt, uint rd, uint shamt, uint funct)
        {
            uint encoding = 0;
            encoding |= rs << 21;
            encoding |= rt << 16;
            encoding |= rd << 11;
            encoding |= shamt << 6;
            encoding |= funct;
            byte[] bytes = BitConverter.GetBytes(encoding);
            //bytes.Reverse();
            return bytes;
        }

        public void InsertHook(ulong targetAddress,ulong freeAddress)
        {
            int targetInstructionSize = 8;
            if (_targetAddress != 0 && _freeAddress != 0)
            {
                Log.Warning("can't run InsertHook on already inserted hook");
                return;
            }
            _isJumptablePatch = false;
            _targetAddress = targetAddress;
            _targetInstructionSize = targetInstructionSize;
            _freeAddress = freeAddress;

            List<byte> jmpBack = ConvertAsm([$"jmp 0x{(_targetAddress + (ulong)_targetInstructionSize):X}", "nop"]); //($"JMP 0x{(_targetAddress + (ulong)_targetInstructionSize):X}");
            _hookSize = (ulong)(_targetInstructionSize + _bytes.Count + jmpBack.Count);

            byte[] freeBytes = Memory.ReadByteArray(_freeAddress, (int)_hookSize);

            if (freeBytes.Any(b => b != 0x00))
            {
                Log.Warning($"CustomHook: Free space at 0x{_freeAddress:X} is not empty!");
                //return;
            }

            byte[] first = Memory.ReadByteArray(_targetAddress, _targetInstructionSize);
            List<byte> jmpto = ConvertAsm([$"jmp 0x{(_freeAddress):X}", "nop"]);

            if (first.SequenceEqual(jmpto))
            {
                Log.Information("hook already inserted from a previous connection");
                return;
            }

            while (jmpto.Count < targetInstructionSize)
            {
                jmpto.Add(0x00);
            }
            Memory.WriteByteArray(_targetAddress, jmpto.ToArray());
            Log.Debug($"jmpto: {Convert.ToHexString([jmpto[0], jmpto[1], jmpto[2], jmpto[3]])}");

            Memory.WriteByteArray(_freeAddress, first);
            Log.Debug($"first: {Convert.ToHexString([first[0], first[1], first[2], first[3]])}");

            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize, _bytes.ToArray());
            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize + (ulong) _bytes.Count, jmpBack.ToArray());

            Log.Debug("Hook is in");
        }

        public void InsertHookInJumptable(ulong jumptableEntryAddress, ulong jumptableExitAddress, ulong freeAddress)
        {
            int targetInstructionSize = 4;
            if (_targetAddress != 0 && _freeAddress != 0)
            {
                Log.Warning("can't run InsertHook on already inserted hook");
                return;
            }
            _isJumptablePatch = true;
            _targetAddress = jumptableEntryAddress;
            _targetInstructionSize = targetInstructionSize;
            _freeAddress = freeAddress;

            List<byte> jmpBack = ConvertAsm([$"jmp 0x{jumptableExitAddress:X}", "nop"]); //($"JMP 0x{(_targetAddress + (ulong)_targetInstructionSize):X}");
            _hookSize = (ulong)(_targetInstructionSize + _bytes.Count + jmpBack.Count);

            byte[] freeBytes = Memory.ReadByteArray(_freeAddress, (int)_hookSize);

            if (freeBytes.Any(b => b != 0x00))
            {
                Log.Warning($"CustomHook: Free space at 0x{_freeAddress:X} is not empty!");
                //return;
            }

            byte[] first = Memory.ReadByteArray(_targetAddress, _targetInstructionSize);
            Memory.WriteByteArray(_freeAddress, first);
            Log.Debug($"first: {Convert.ToHexString([first[0], first[1], first[2], first[3]])}");

            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize, _bytes.ToArray());
            Memory.WriteByteArray(_freeAddress + (ulong)_targetInstructionSize + (ulong)_bytes.Count, jmpBack.ToArray());
            Memory.WriteByteArray(_targetAddress, BitConverter.GetBytes((uint)(0x80000000 | freeAddress)));

            Log.Debug("Hook is in");
        }

        public void RemoveHook()
        {
            if (_targetAddress == 0 && _freeAddress == 0)
            {
                Log.Warning("can't run RemoveHook on uninserted hook");
                return;
            }
            byte[] originalInstruction = Memory.ReadByteArray(_freeAddress, (int)_targetInstructionSize);
            if (!originalInstruction.All(b => b == 0x00)) {
                Memory.WriteByteArray(_targetAddress, originalInstruction);
            }
            Memory.WriteByteArray(_freeAddress, new byte[_hookSize]);

            _hookSize = 0;
            _targetAddress = 0;
            _targetInstructionSize = 0;
            _freeAddress = 0;
        }
    }
}
