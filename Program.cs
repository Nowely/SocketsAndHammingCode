using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SAHC;

//var udp = SocketUdp.Create();
//udp.Start();
var a = new HammingCode(54);
var b = a.Encode("Hello World!");
var c = a.Decode(b);
Console.WriteLine(c);