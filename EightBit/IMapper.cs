// <copyright file="IMapper.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public interface IMapper
    {
        MemoryMapping Mapping(ushort absolute);
    }
}
