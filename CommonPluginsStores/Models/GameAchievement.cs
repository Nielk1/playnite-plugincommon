﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class GameAchievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime DateUnlocked { get; set; }
        public float Percent { get; set; }
    }
}
