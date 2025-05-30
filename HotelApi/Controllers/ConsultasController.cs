﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelApi.Data;
using HotelApi.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using HotelApi.DTOs;

namespace HotelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultasController : ControllerBase
    {
        private readonly HotelApiContext _context;

        public ConsultasController(HotelApiContext context)
        {
            _context = context;
        }

        // GET: api/Consultas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultaDTO>>> GetConsulta()
        {
            var consultas = await _context.Consulta.Where(c => c.Activo).ToListAsync();
            return consultas.Select(ToDTO).ToList();
        }

        // GET: api/Consultas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ConsultaDTO>> GetConsulta(int id)
        {
           
            var consulta = await _context.Consulta.Where(c => c.Activo && c.Id == id).FirstOrDefaultAsync();

            if (consulta == null)
            {
                return NotFound();
            }

            return ToDTO(consulta);
        }

        // POST: api/Consultas
        [HttpPost]
        public async Task<ActionResult<ConsultaDTO>> PostConsulta(ConsultaDTO consultaDTO)
        {
            var consulta = new Consulta
            {
                Nombre = consultaDTO.Nombre,
                Email = consultaDTO.Email,
                Telefono = consultaDTO.Telefono,
                Mensaje = consultaDTO.Mensaje,
                Creacion = DateTime.Now,
                Actualizacion = DateTime.Now,
                Activo = consultaDTO.Activo
            };
            _context.Consulta.Add(consulta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConsulta), new { id = consulta.Id }, ToDTO(consulta));
        }


        // POST: api/Consultas/public ; crea la solicitud asociada
        [HttpPost("public")]
        public async Task<ActionResult<ConsultaDTO>> PostPublicConsulta(ConsultaDTO consultaDTO)
        {
            var consulta = new Consulta
            {
                Nombre = consultaDTO.Nombre,
                Email = consultaDTO.Email,
                Telefono = consultaDTO.Telefono,
                Mensaje = consultaDTO.Mensaje,
                Creacion = DateTime.Now,
                Actualizacion = DateTime.Now,
                Activo = true
            };
            _context.Consulta.Add(consulta);

            var solicitud = new Solicitud
            {
                ConsultaId = consulta.Id,
                Consulta = consulta,
                Tipo = "Consulta",
                EsLeida = false,
                Creacion = DateTime.Now,
                Actualizacion = DateTime.Now,
                Activo = true
            };
            _context.Solicitud.Add(solicitud);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConsulta), new { id = consulta.Id }, ToDTO(consulta));
        }

        // PUT: api/Consultas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConsulta(int id, ConsultaDTO consultaDTO)
        {
            if (id != consultaDTO.Id)
            {
                return BadRequest();
            }

            var consulta = await _context.Consulta.FindAsync(id);
            if (consulta == null)
            {
                return NotFound();
            }

            consulta.Nombre = consultaDTO.Nombre;
            consulta.Email = consultaDTO.Email;
            consulta.Telefono = consultaDTO.Telefono;
            consulta.Mensaje = consultaDTO.Mensaje;
            consulta.Actualizacion = DateTime.Now;
            consulta.Activo = consultaDTO.Activo;

            _context.Entry(consulta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConsultaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Consultas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsulta(int id)
        {
            var consulta = await _context.Consulta.FindAsync(id);
            if (consulta == null || !consulta.Activo)
            {
                return NotFound();
            }

            consulta.Activo = false;
            consulta.Actualizacion = DateTime.Now;

            _context.Entry(consulta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConsultaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool ConsultaExists(int id)
        {
            return _context.Consulta.Any(e => e.Id == id);
        }
        public static ConsultaDTO ToDTO(Consulta consulta)
        {
            return new ConsultaDTO
            {
                Id = consulta.Id,
                Nombre = consulta.Nombre,
                Email = consulta.Email,
                Telefono = consulta.Telefono,
                Mensaje = consulta.Mensaje,
                Activo = consulta.Activo
            };
        }
    }
}
