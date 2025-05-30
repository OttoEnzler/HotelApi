﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelApi.Data;
using HotelApi.Models;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;
using HotelApi.DTOs;

namespace HotelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagenHabitacionsController : ControllerBase
    {
        private readonly HotelApiContext _context;

        public ImagenHabitacionsController(HotelApiContext context)
        {
            _context = context;
        }

        // GET: api/ImagenHabitacions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImagenHabitacionDTO>>> GetImagenHabitacion()
        {
            var imagenes = await _context.ImagenHabitacion.Where(i => i.Activo).ToListAsync();
            var imgDTO = imagenes.Select(i => ToDTO(i));
            return Ok(imgDTO);
        }

        // GET: api/ImagenHabitacions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ImagenHabitacionDTO>> GetImagenHabitacion(int id)
        {
            var imagenHabitacion = await _context.ImagenHabitacion.Where(i => i.Activo && i.Id == id).FirstOrDefaultAsync();

            if (imagenHabitacion == null)
            {
                return NotFound();
            }

            return ToDTO(imagenHabitacion);
        }

        // GET: ImagenesHabitaciones/tipo/{id}
        [HttpGet("tipo/{id}")]
        public async Task<ActionResult<IEnumerable<ImagenHabitacionDTO>>> GetImagenesPorTipo(int id)
        {
            var imagenes = await _context.ImagenHabitacion
                .Where(img => img.TipoHabitacionId == id)
                .Select(img => new ImagenHabitacionDTO
                {
                    Id = img.Id,
                    Url = img.Url,
                    TipoHabitacionId = img.TipoHabitacionId
                })
                .ToListAsync();

            return Ok(imagenes);
        }

            // PUT: api/ImagenHabitacions/5
            // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
            [HttpPut("{id}")]
        public async Task<IActionResult> PutImagenHabitacion(int id, ImagenHabitacionDTO imagenHabitacionDTO)
        {
            if (id != imagenHabitacionDTO.Id)
            {
                return BadRequest();
            }

            var imagenHabitacion = await _context.ImagenHabitacion.FindAsync(id);
            if (imagenHabitacion == null)
            {
                return NotFound();
            }

            imagenHabitacion.TipoHabitacionId = imagenHabitacionDTO.TipoHabitacionId;
            imagenHabitacion.Url = imagenHabitacionDTO.Url;
            imagenHabitacion.Actualizacion = DateTime.Now;

            _context.Entry(imagenHabitacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ImagenHabitacionExists(id))
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

        // POST: api/ImagenHabitacions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ImagenHabitacionDTO>> PostImagenHabitacion(ImagenHabitacionDTO imagenHabitacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var imagenHabitacion = new ImagenHabitacion
            {
                TipoHabitacionId = imagenHabitacionDTO.TipoHabitacionId,
                Url = imagenHabitacionDTO.Url,
                Creacion = DateTime.Now,
                Actualizacion = DateTime.Now,
                Activo = true
            };
            _context.ImagenHabitacion.Add(imagenHabitacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImagenHabitacion), new { id = imagenHabitacion.Id }, ToDTO(imagenHabitacion));
        }

        // DELETE: api/ImagenHabitacions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImagenHabitacion(int id)
        {
            var imagenHabitacion = await _context.ImagenHabitacion.FindAsync(id);
            if (imagenHabitacion == null)
            {
                return NotFound();
            }

            // Eliminar archivo físico
            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagenPath = Path.Combine(wwwRootPath, "imagenes", Path.GetFileName(imagenHabitacion.Url));
            if (System.IO.File.Exists(imagenPath))
            {
                System.IO.File.Delete(imagenPath);
            }

            // Eliminar de la base de datos
            _context.ImagenHabitacion.Remove(imagenHabitacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ImagenHabitacionExists(int id)
        {
            return _context.ImagenHabitacion.Any(e => e.Id == id);
        }

        private static ImagenHabitacionDTO ToDTO(ImagenHabitacion imagenHabitacion)
        {
            return new ImagenHabitacionDTO
            {
                Id = imagenHabitacion.Id,
                TipoHabitacionId = imagenHabitacion.TipoHabitacionId,
                Url = imagenHabitacion.Url
            };
        }
    }
}

