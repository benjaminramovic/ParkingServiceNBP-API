﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ParkEasyNBP.API.FilteringSortingPaging;
using ParkEasyNBP.API.Mediator.Query;
using ParkEasyNBP.Application.DTOs;
using ParkEasyNBP.Domain.Interfaces;
using ParkEasyNBP.Domain.Models;
using ParkEasyNBP.Domain.ModelsMongoDB;
using ParkEasyNBP.Infrastructure.MongoDB;
using Repository.Repositories;
using System.Linq.Expressions;
//using System.Security.Policy;

namespace ParkEasyNBP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZoneController : ControllerBase
    {
        private readonly IZoneRepository service;
        private readonly IMapper mapper;
        private readonly MongoService mongoService;

        public ZoneController(IZoneRepository service, IMapper mapper, MongoService mongoService)
        {
            this.service = service;
            this.mapper = mapper;
            this.mongoService = mongoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllZones(/*[FromQuery]ZoneQueryObject qo*/)
        {

          /*  var list = await service.GetAll();
            var list2 = mapper.Map<IEnumerable<ZonesDTO>>(list);

            Dictionary<string, Expression<Func<ZonesDTO, object>>> columnMaps = new Dictionary<string, Expression<Func<ZonesDTO, object>>>
            {
                ["Name"] = c => c.Name
            };
            if (!qo.Name.IsNullOrEmpty())
                list2 = list2.Where(c => c.Name == qo.Name).AsQueryable();*/

            //list2 = list2.AsQueryable();/*ApplySorting<ZonesDTO>(qo, columnMaps).ApplyPaging(qo);*/
            return Ok(mongoService.GetAll());
            
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var obj = await service.Get(id);
            if(obj == null)
            {
                return NotFound("Nije pronadjena zona.");
            }
            return Ok(obj);
        }
        [HttpPost]
        public async Task<IActionResult> AddZone([FromBody] ZoneMongoDTO zone)
        {
            /*  var zona = mapper.Map<Domain.Models.Zone>(zone);
              await service.Create(zona);*/
            var zona = await mongoService.Create(mapper.Map<ZoneMongoDB>(zone));
          
            return Ok(zona);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteZone([FromRoute]int id)
        {
            var response = await service.Delete(id);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZone(int id, [FromBody] ZoneCreateDTO zonee)
        {
            var zone = mapper.Map<Zone>(zonee);
            zone.Id = id;

            if (zone == null || zone.Id != id)
            {
                return BadRequest();
            }

            var updatedZone = await service.Update(id, zone);
            if (updatedZone == null)
            {
                return NotFound();
            }

            return Ok(updatedZone);
        }
    }
}
