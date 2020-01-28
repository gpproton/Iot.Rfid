﻿//
// KingJoinProtocol.cs
//
// Author:
//       Godwin peter .O <me@godwin.dev>
//
// Copyright (c) 2020 MIT
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Numerics;
using NLog.Fluent;
using uhf_rfid_catch.Data;
using uhf_rfid_catch.Helpers;
using uhf_rfid_catch.Models;

namespace uhf_rfid_catch.Protocols.Readers
{
    public class KingJoinProtocol : BaseProtocol
    {
//        private ByteAssist _assist;
        private string _tagData;
        private string _tagType;
        

        public KingJoinProtocol()
        {
            DataLength = 32;
        }
        
        public new const byte START_RESPONSE_BYTE = 0xCC;
        public new const byte START_COMMAND_BYTE = 0x7C;
        
        // Control identification data description
        public const byte ISO_SINGLE = 0x01;
        public const byte ISO_MEM = 0x02;
        
        public const byte CID1_EPC_SINGLE = 0x10;
        public const byte CID1_EPC_MULTI = 0x11;
        public const byte CID1_EPC_MEM = 0x12;
        
        // Control identification data actions.
        public const byte CID2_GET = 0x32;
        public const byte CID2_SET= 0x31;
        public const byte CID2_SUPER_GET = 0x22;
        public const byte CID2_SUPER_SET = 0x21;
        
        // Return code/status types
        public const byte RTN_SUCCESS = 0x00;
        public const byte RTN_FAIL = 0x01;
        public const byte RTN_AUTO = 0x32;

        public override Scan DecodeData()
        {
            bool _continue = START_RESPONSE_BYTE == _assist.pick( ReceivedData, 0);
            
            if (_continue
            && _assist.pick(ReceivedData, 3) == CID1_EPC_SINGLE
            && _assist.pick(ReceivedData,4) == CID2_GET)
            {
                // EPC (Gen 2) Single tag identity.
                TagDataLength = Convert.ToInt32(_assist.pick(ReceivedData,5));
                _tagType = "EPC";
                
                _tagData = _session.GetTagID(_assist.between(ReceivedData,10, 5 + TagDataLength));
            }
            else if (_continue
                     && _assist.pick(ReceivedData, 3) == CID1_EPC_MULTI
                     && _assist.pick(ReceivedData, 5) == CID2_GET)
            {
                // EPC (Gen 2) Multi tag identity.
                
                _continue = false;
            }
            else if (_continue
                     && _assist.pick(ReceivedData, 3) == CID1_EPC_MEM
                     && _assist.pick(ReceivedData, 4) == CID2_GET)
            {
                // EPC (Gen 2) Memory bank read.
                TagDataLength = Convert.ToInt32(_assist.pick(ReceivedData,6));
                _tagType = "EPC";
                
                _tagData = _session.GetTagID(_assist.between(ReceivedData,10, 5 + TagDataLength));
                
                
                _continue = false;
            }
            else if (_continue
                     && _assist.pick(ReceivedData, 3) == ISO_SINGLE
                     && _assist.pick(ReceivedData, 4) == CID2_GET)
            {
                // ISO 18000-6B Single tag read.
                TagDataLength = Convert.ToInt32(_assist.pick(ReceivedData,6));
                _tagType = "ISO";
                
                _tagData = _session.GetTagID(_assist.between(ReceivedData,10, 5 + TagDataLength));
            }
            else if (_continue
                     && _assist.pick(ReceivedData, 3) == ISO_MEM
                     && _assist.pick(ReceivedData, 4) == CID2_GET)
            {
                // ISO 18000-6B Memory bank read.
                TagDataLength = Convert.ToInt32(_assist.pick(ReceivedData,6));
                _tagType = "ISO";
                
                _tagData = _session.GetTagID(_assist.between(ReceivedData,10, 5 + TagDataLength));
                _continue = false;
            }
            else
            {
                _continue = false;
            }
            
            
            var scn = new Scan
            {
                CaptureTime = DateTime.Now
            };
            
            _context.Database.EnsureCreated();
            
            Reader readerid;
            if ((readerid = _context.Readers.FirstOrDefault(e => e.UniqueId == _config.IOT_UNIQUE_ID)) != null)
            {
                // Get already entered Unique Id for Reader.
                scn.ReaderId = readerid.Id;
            }
            else
            {
                // Enter new Reader details.
                // TODO: Make protocol and mode detection more dynamic.
                scn.Reader = new Reader { UniqueId = _config.IOT_UNIQUE_ID, Mode = _session.GetMode(), Protocol = _config.IOT_PROTOCOL };
            }

            Tag tagid;
            if ((tagid = _context.Tags.FirstOrDefault(e => e.UniqueId == _tagData)) != null)
            {
                // Get already entered Unique Id for Tag.
                scn.TagId = tagid.Id;
            }
            else
            {
                // Enter new Tag details.
                scn.Tag = new Tag
                {
                    UniqueId = _tagData,
                    Type = _tagType,
                    LastMode = "Unknown"
                };
            }
            
            return scn;
        }
        
    }
}