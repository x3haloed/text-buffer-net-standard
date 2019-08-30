using System.IO;
using WebAssembly;

namespace TextBuffer
{
    public static class WebAssemblyModuleManager
    {
        public static string BinaryPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "binfile.wasm" );
        public static Module TextBufferModule { get; } = Module.ReadFromBinary(BinaryPath);
    }
}
