﻿using ParkEasyNBP.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEasyNBP.Domain.Interfaces
{
    public interface IVehicleRepository : IRepository<Vehicle>
    {
        Task<Vehicle> VehicleOfOwner(string id);
        Task<IEnumerable<Vehicle>> Search(string regNumber);
        

    }
}
