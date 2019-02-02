namespace EightBit
{
    public interface IMapper
    {
        MemoryMapping Mapping(Register16 address);
    }
}
