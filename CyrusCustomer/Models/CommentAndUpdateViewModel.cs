﻿using CyrusCustomer.Domain.Models;

namespace CyrusCustomer.Models
{
    public class CommentAndUpdateViewModel
    {
        public int Id { get; set; }
        public string Comments { get; set; } = "null";
        public bool IsUpdated { get; set; }

        public CustomerStatus Status { get; set; }
        public string? By { get; set; }

    }
}
