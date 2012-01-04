﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Sharpcraft.Protocol
{
	public enum EndianType
	{
		LittleEndian,
		BigEndian
	}

	public class Protocol
	{
		private readonly TcpClient _client = new TcpClient();
		private readonly NetworkStream _stream;

		public Protocol(string server, int port)
		{
			_client.Connect(server, port);
			_stream = _client.GetStream();
		}


		public byte[] StringToBytes(string str)
		{
			byte[] strLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(str.Length));
			List<Byte> bytes = strLength.ToList();

			byte[] bteString = Encoding.BigEndianUnicode.GetBytes(str);
			bytes.AddRange(bteString);

			return bytes.ToArray();
		}

		public string BytesToString(byte[] bytes) {
			byte[] bteStrLength = { bytes[0], bytes[1] };
			int strLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bteStrLength, 0));

			string str = "";

			for (short s = 1; s < strLength + 1; s++)
			{
				byte[] tmp = { bytes[s * 2], bytes[(s * 2) + 1] };
				str += Encoding.BigEndianUnicode.GetString(tmp);
			}

			return str;
		}

		private string ReadString()
		{
			byte[] bteStringLength = { (byte)_stream.ReadByte(), (byte)_stream.ReadByte() };
			short stringLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bteStringLength, 0));
			string str = "";
			for (short s = 0; s < stringLength; s++)
			{
				byte[] bte = { (byte)_stream.ReadByte(), (byte)_stream.ReadByte() };
				str += Encoding.BigEndianUnicode.GetString(bte);
			}

			return str;
		}

		private Int16 ReadInt16()
		{
			var bte = new byte[2];
			_stream.Read(bte, 0, bte.Length);
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bte, 0));
		}

		private Int32 ReadInt32()
		{
			var bte = new byte[4];
			_stream.Read(bte, 0, bte.Length);
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bte, 0));
		}

		private Int64 ReadInt64()
		{
			var bte = new byte[8];
			_stream.Read(bte, 0, bte.Length);
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bte, 0));
		}

		private void StreamSkip(int amount)
		{
			for (int i = 0; i < amount; i++)
				_stream.ReadByte();
		}

		public Packet GetPacket()
		{
			var packetID = (byte)_stream.ReadByte();
			Packet pack = null;

			if (packetID == 0x00) // Keep alive
			{
				var packet = new Packet0 {PacketID = 0x00};
				packet.KeepAliveID = ReadInt32();
				pack = packet;
			}
			else if (packetID == 0x01) // Login Request
			{
				var packet = new Packet1 {PacketID = 0x01};

				packet.EntityID = ReadInt32();
				StreamSkip(2);
				packet.MapSeed = ReadInt64();
				packet.Gamemode = ReadInt32();
				packet.Dimension = (sbyte)_stream.ReadByte();
				packet.Difficulty = (sbyte)_stream.ReadByte();
				packet.WorldHeight = (byte)_stream.ReadByte();
				packet.MaxPlayers = (byte)_stream.ReadByte();

				pack = packet;
			}
			else if (packetID == 0x02)
			{
				var packet = new Packet2 {PacketID = 0x02};
				packet.ConnectionHash = ReadString();
				pack = packet;
			}
			else if (packetID == 0x03)
			{
				var packet = new Packet3 {PacketID = 0x03};
				packet.Message = ReadString();
				pack = packet;
			}
			else if (packetID == 0x04)
			{
				var packet = new Packet4 {PacketID = 0x04};
				packet.Time = ReadInt32();
				pack = packet;
			}
			else if (packetID == 0x05)
			{
				var packet = new Packet5 {PacketID = 0x05};

				packet.EntityID = ReadInt32();
				packet.Slot = ReadInt16();
				packet.ItemID = ReadInt16();
				packet.Damage = ReadInt16();

				pack = packet;
			}

			return pack;
		}
			
		// Packet 0x01
		public void PacketLoginRequest(int version, string username)
		{
			// Write the Packet ID (0x01)
			_stream.WriteByte(0x01);

			// Write the protocol version (22 (0x16) for 1.0.0)
			int beVersion = IPAddress.HostToNetworkOrder(version);
			byte[] bteVersion = BitConverter.GetBytes(beVersion);
			_stream.Write(bteVersion, 0, bteVersion.Length);

			// Write the username
			_stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)username.Length)), 0, 2);
			byte[] bteUsername = Encoding.BigEndianUnicode.GetBytes(username);
			_stream.Write(bteUsername, 0, bteUsername.Length);

			// Write NotUsed 1
			byte[] bteNotUsed1 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			_stream.Write(bteNotUsed1, 0, bteNotUsed1.Length);

			// Write NotUsed 2
			byte[] bteNotUsed2 = { 0x00, 0x00, 0x00, 0x00 };
			_stream.Write(bteNotUsed2, 0, bteNotUsed2.Length);

			// Write NotUsed 3 through 6
			_stream.WriteByte(0x00);
			_stream.WriteByte(0x00);
			_stream.WriteByte(0x00);
			_stream.WriteByte(0x00);

			// Flush the stream
			_stream.Flush();
		}

		// Packet 0x02
		public void PacketHandshake(string username)
		{
			// Send the packet ID
			_stream.WriteByte(0x02);

			// Write the username
			_stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)username.Length)), 0, 2);
			byte[] str = Encoding.BigEndianUnicode.GetBytes(username);
			_stream.Write(str, 0, str.Length);

			// Flush the stream
			_stream.Flush();
		}

	}
}
