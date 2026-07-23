namespace Atria.Pipeline.Models;

public sealed record StoredBlock<T>(T Data, int SizeBytes);
