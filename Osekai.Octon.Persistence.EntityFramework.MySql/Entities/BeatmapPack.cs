﻿using Osekai.Octon.Models;
using Osekai.Octon.Persistence.EntityFramework.MySql.Dtos;

namespace Osekai.Octon.Persistence.EntityFramework.MySql.Entities;

internal sealed class BeatmapPack: IReadOnlyBeatmapPack
{
    public int Id { get; set; }
    public int BeatmapCount { get; set; }

    public ICollection<BeatmapPackForMedal> MedalsForBeatmapPack { get; set; } = null!;

    public BeatmapPackDto ToDto()
    {
        return new BeatmapPackDto(Id, BeatmapCount);
    }
}