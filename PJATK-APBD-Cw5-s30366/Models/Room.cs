using System;
using System.Collections.Generic;

namespace PJATK_APBD_Cw5_s30366.Models;

public partial class Room
{
    public string Id { get; set; } = null!;

    public int WardId { get; set; }

    public bool HasTv { get; set; }

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();

    public virtual Ward Ward { get; set; } = null!;
}
