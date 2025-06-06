﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelApi.Data;
using HotelApi.Models;
using Microsoft.AspNetCore.Authorization;
using HotelApi.DTOs;
using System.Net.Mail;
using System.Net;
using HotelApi.DTOs.Request;

namespace HotelApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly HotelApiContext _context;

        public ReservasController(HotelApiContext context)
        {
            _context = context;
        }

        // GET: api/Reservas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservaDTO>>> GetReservas()
        {
            // obtener solo los activos
            var res = await _context.Reserva
            .Where(r => r.Activo)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Habitacion)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.TipoHabitacion)
            .Include(r => r.Cliente)
            .ToListAsync();

            var resDtos = res.Select(r => ToDTO(r));
            return Ok(resDtos);
        }

        // GET: api/Reservas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservaDTO>> GetReserva(int id)
        {
            var reserva = await _context.Reserva
            .Include(r => r.Detalles)
                .ThenInclude (d => d.Habitacion)
             .Include(r => r.Detalles)
                .ThenInclude(d => d.TipoHabitacion)
            .Include(r => r.Cliente)
            .Where(r => r.Activo && r.Id == id)
            .FirstOrDefaultAsync();

            if (reserva == null)
            {
                return NotFound();
            }

            return ToDTO(reserva);
        }

        //obtener reserva con determinado codigo
        // GET: api/Reservas/code/RES001
        [HttpGet("/code/{codigo}")]
        public async Task<ActionResult<IEnumerable<ReservaDTO>>> GetReservaPorCodigo(string codigo)
        {
            var res = await _context.Reserva
            .Where(r => r.Codigo == codigo && r.EstadoId == 1)
            .Include(r => r.Detalles)
            .ToListAsync();

            if (res == null)
            {
                return NotFound();
            }

            var resDtos = res.Select(r => ToDTO(r));
            return Ok(resDtos);
        }

        //obtener reservas de un cliente con determinado id
        // GET: api/Reservas/cliente/5
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<ReservaDTO>>> GetReservasCliente(int clienteId)
        {
            var res = await _context.Reserva
            .Where(r => r.ClienteId == clienteId && r.Activo)
            .OrderByDescending(r => r.FechaIngreso)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Habitacion)
             .Include(r => r.Detalles)
                .ThenInclude(d => d.TipoHabitacion)
            .ToListAsync();

            if (res == null)
            {
                return NotFound();
            }

            var resDtos = res.Select(r => ToDTO(r));
            return Ok(resDtos);
        }
        // obtener ultima reserva de un cliente con determinado id
        // GET: api/Reservas/cliente/5/ultima
        [HttpGet("cliente/{clienteId}/ultima")]
        public async Task<ActionResult<ReservaDTO>> GetUltimaReserva(int clienteId)
        {
            var reserva = await _context.Reserva
            .Where(r => r.ClienteId == clienteId && r.Activo)
            .OrderByDescending(r => r.FechaIngreso)
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync();

            if (reserva == null)
            {
                return NotFound();
            }

            return ToDTO(reserva);
        }

        // PUT: api/Reservas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReserva(int id, ReservaDTO resDTO)
        {
            if (id != resDTO.Id)
                return BadRequest();

            var res = await _context.Reserva
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (res == null)
                return NotFound();

            // Actualizar campos simples
            res.ClienteId = resDTO.ClienteId;
            res.Codigo = resDTO.Codigo;
            res.FechaIngreso = resDTO.FechaIngreso;
            res.FechaSalida = resDTO.FechaSalida;
            res.LlegadaEstimada = resDTO.LlegadaEstimada;
            res.Comentarios = resDTO.Comentarios;
            res.EstadoId = resDTO.EstadoId;
            res.Actualizacion = DateTime.Now;

            var detallesDTO = resDTO.Detalles ?? new List<DetalleReservaDTO>();
            var idsDTO = detallesDTO.Select(d => d.Id).ToHashSet();

            // Desactivar detalles que no vienen en el DTO (soft delete)
            foreach (var detalle in res.Detalles)
            {
                if (!idsDTO.Contains(detalle.Id))
                {
                    detalle.Activo = false;
                }
            }

            // Agregar o actualizar detalles del DTO
            foreach (var dto in detallesDTO)
            {
                var existente = res.Detalles.FirstOrDefault(d => d.Id == dto.Id);
                if (existente != null)
                {
                    // Actualizar
                    existente.HabitacionId = dto.HabitacionId;
                    existente.TipoHabitacionId = dto.TipoHabitacionId;
                    existente.CantidadAdultos = dto.CantidadAdultos;
                    existente.CantidadNinhos = dto.CantidadNinhos;
                    existente.PensionId = dto.PensionId;
                    existente.Activo = true; // Revivir si estaba inactivo
                }
                else
                {
                    // Nuevo detalle
                    res.Detalles.Add(new DetalleReserva
                    {
                        HabitacionId = dto.HabitacionId,
                        TipoHabitacionId = dto.TipoHabitacionId,
                        CantidadAdultos = dto.CantidadAdultos,
                        CantidadNinhos = dto.CantidadNinhos,
                        PensionId = dto.PensionId,
                        Activo = true
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }


        // POST: api/Reservas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ReservaDTO>> PostReserva(ReservaDTO resDto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Mensaje = "Modelo inválido", Errores = errores });
            }

            var codigo = await GenerarCodigoUnicoAsync();
            var res = new Reserva
            {
                ClienteId = resDto.ClienteId,
                Codigo = codigo,
                FechaIngreso = resDto.FechaIngreso,
                FechaSalida = resDto.FechaSalida,
                LlegadaEstimada = resDto.LlegadaEstimada,
                Comentarios = resDto.Comentarios,
                EstadoId = resDto.EstadoId,
                Creacion = DateTime.Now,
                Actualizacion = DateTime.Now,
                Activo = true,
            };

            if (resDto.Detalles != null)
            {
                res.Detalles = resDto.Detalles.Select(d => new DetalleReserva
                {
                    HabitacionId = d.HabitacionId,
                    TipoHabitacionId = d.TipoHabitacionId,
                    CantidadAdultos = d.CantidadAdultos,
                    CantidadNinhos = d.CantidadNinhos,
                    PensionId = d.PensionId,
                    Activo = d.Activo,
                    Creacion = DateTime.Now,
                    Actualizacion = DateTime.Now
                }).ToList();
            }

            _context.Reserva.Add(res);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReserva", new { id = res.Id }, resDto);
        }

        // POST: api/Reservas/Cliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Cliente")]
        public async Task<ActionResult<ReservaDTO>> PostReservaCliente(ReservaClienteDTO resDto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Mensaje = "Modelo inválido", Errores = errores });
            }

            Cliente clienteParaReserva;
            var infoClienteDto = resDto.InformacionCliente;
            var infoReservaDto = resDto.InformacionReserva;

            clienteParaReserva = await _context.Cliente
                                   .FirstOrDefaultAsync(c => c.NumDocumento == infoClienteDto.NumDocumento);

            bool clienteEsNuevo = false;
            if (clienteParaReserva == null)
            {
                clienteParaReserva = new Cliente
                {
                    Nombre = infoClienteDto.Nombre,
                    Apellido = infoClienteDto.Apellido,
                    Email = infoClienteDto.Email,
                    Telefono = infoClienteDto.Telefono,
                    NumDocumento = infoClienteDto.NumDocumento,
                    Ruc = infoClienteDto.Ruc,
                    TipoDocumentoId = infoClienteDto.TipoDocumentoId,
                    Nacionalidad = infoClienteDto.Nacionalidad,
                    Comentarios = infoClienteDto.Comentarios,
                    Activo = true,
                    Creacion = DateTime.UtcNow
                };
                _context.Cliente.Add(clienteParaReserva);
                clienteEsNuevo = true;
            }   

            var codigo = await GenerarCodigoUnicoAsync();
            var res = new Reserva
            {
                Cliente = clienteParaReserva,
                Codigo = codigo,
                FechaIngreso = infoReservaDto.FechaIngreso,
                FechaSalida = infoReservaDto.FechaSalida,
                LlegadaEstimada = infoReservaDto.LlegadaEstimada,
                Comentarios = infoReservaDto.Comentarios,
                EstadoId = infoReservaDto.EstadoId,
                Creacion = DateTime.UtcNow,
                Actualizacion = DateTime.UtcNow,
                Activo = true, // O un valor que venga del DTO si es configurable
            };

            if (infoReservaDto.Detalles != null)
            {
                res.Detalles = infoReservaDto.Detalles.Select(d => new DetalleReserva
                {
                    HabitacionId = d.HabitacionId,
                    TipoHabitacionId = d.TipoHabitacionId,
                    CantidadAdultos = d.CantidadAdultos,
                    CantidadNinhos = d.CantidadNinhos,
                    PensionId = d.PensionId,
                    Activo = d.Activo,
                    Creacion = DateTime.Now,
                    Actualizacion = DateTime.Now
                }).ToList();
            }

            _context.Reserva.Add(res);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Mensaje = "Error al guardar los datos en la base de datos.", Detalle = ex.InnerException?.Message ?? ex.Message });
            }

            // Preparar una respuesta más útil
            var respuestaDto = new
            {
                mensaje = "Reserva creada exitosamente.",
                reservaId = res.Id,
                codigoReserva = res.Codigo,
                clienteId = clienteParaReserva.Id,
                clienteNuevo = clienteEsNuevo,
                emailCliente = clienteParaReserva.Email,
                fechaIngreso = res.FechaIngreso,
                fechaSalida = res.FechaSalida
            };

            return CreatedAtAction("GetReserva", new { id = res.Id }, respuestaDto);
        }

        // CONFIRMAR RESERVA, ASIGNAR HABITACIONES Y MANDAR EMAIL DE CONFIRMACION
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmarReserva(int id, ReservaDTO resDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var res = await _context.Reserva
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (res == null)
                return NotFound();

            var detallesDTO = resDTO.Detalles ?? new List<DetalleReservaDTO>();
            var idsDTO = detallesDTO.Select(d => d.Id).ToHashSet();

            // Desactivar detalles que no están en el DTO
            foreach (var detalle in res.Detalles)
            {
                if (!idsDTO.Contains(detalle.Id))
                {
                    detalle.Activo = false;
                }
            }

            // Agregar o actualizar detalles del DTO
            foreach (var dto in detallesDTO)
            {
                if (dto.HabitacionId == null)
                {
                    return BadRequest("Cada detalle debe tener una habitación asignada.");
                }

                var existente = res.Detalles.FirstOrDefault(d => d.Id == dto.Id);
                if (existente != null)
                {
                    // Actualizar
                    existente.HabitacionId = dto.HabitacionId;
                    existente.TipoHabitacionId = dto.TipoHabitacionId;
                    existente.CantidadAdultos = dto.CantidadAdultos;
                    existente.CantidadNinhos = dto.CantidadNinhos;
                    existente.PensionId = dto.PensionId;
                    existente.Activo = true;
                }
                else
                {
                    // Agregar nuevo
                    res.Detalles.Add(new DetalleReserva
                    {
                        HabitacionId = dto.HabitacionId,
                        TipoHabitacionId = dto.TipoHabitacionId,
                        CantidadAdultos = dto.CantidadAdultos,
                        CantidadNinhos = dto.CantidadNinhos,
                        PensionId = dto.PensionId,
                        Activo = true
                    });
                }
            }

            // Validar que haya al menos un detalle activo
            if (!res.Detalles.Any(d => d.Activo && d.HabitacionId != null))
            {
                return BadRequest("Debe asignar al menos una habitación activa para confirmar la reserva.");
            }

            // Confirmar reserva
            res.EstadoId = 2; // Confirmada
            res.Actualizacion = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (!ReservaExists(id))
                    return NotFound();
                else
                    throw;
            }

            // Enviar email de confirmación
            var cliente = await _context.Cliente.FindAsync(resDTO.ClienteId);
            if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Email))
            {
                var nombreCliente = cliente.Nombre + " " + cliente.Apellido;
                try
                {
                    EnviarEmailConfirmacion(resDTO, nombreCliente, cliente.Email, res.Codigo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar el email: {ex.Message}");
                }
            }

            return NoContent();
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RechazarReserva(int id)
        {

            var res = await _context.Reserva.FirstOrDefaultAsync(r => r.Id == id);

            if (res == null)
                return NotFound();

            // cambiar a estado "Rechazada"
            res.EstadoId = 6;
            res.Actualizacion = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }



        [HttpPost("asignarHabitaciones")]
        public async Task<IActionResult> AsignarHabitaciones([FromBody] AsignarHabitacionesRequest request)
        {
            foreach (var asignacion in request.Asignaciones)
            {
                var detalle = await _context.DetalleReserva
                    .FirstOrDefaultAsync(d => d.Id == asignacion.DetalleReservaId && d.ReservaId == request.ReservaId);

                if (detalle == null)
                {
                    return NotFound($"No se encontró el detalle con ID {asignacion.DetalleReservaId} para la reserva {request.ReservaId}");
                }

                detalle.HabitacionId = asignacion.HabitacionId;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Habitaciones asignadas correctamente." });
        }


        // DELETE: api/Reservas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva == null)
            {
                return NotFound();
            }

            reserva.Activo = false;
            reserva.Actualizacion = DateTime.Now;

            _context.Entry(reserva).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(id))
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

        private bool ReservaExists(int id)
        {
            return _context.Reserva.Any(e => e.Id == id);
        }

        private async Task<string> GenerarCodigoUnicoAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            string codigo;
            bool existe;

            do
            {
                // generar el codigo unico
                codigo = "RES-" + new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                // verificar si el codigo ya existia
                existe = await _context.Reserva.AnyAsync(r => r.Codigo == codigo);
            }
            while (existe);

            return codigo;
        }

        [HttpGet("disponibles/{reservaId}")]
        public async Task<ActionResult<IEnumerable<HabitacionDTO>>> BuscarDisponibles(int reservaId)
        {
            var reserva = await _context.Reserva
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == reservaId && r.Activo);

            if (reserva == null)
                return NotFound("Reserva no encontrada o inactiva.");

            if (reserva.Detalles == null || reserva.Detalles.Count == 0)
                return BadRequest("La reserva no contiene detalles.");

            var habitacionesDisponibles = new List<Habitacion>();

            foreach (var detalle in reserva.Detalles)
            {
                List<Habitacion> disponibles = new();

                if (detalle.TipoHabitacionId != null)
                {
                    var capacidadRequerida = detalle.CantidadAdultos + detalle.CantidadNinhos;

                    var habitaciones = await _context.Habitacion
                    .Include(h => h.TipoHabitacion)
                    .Where(h => h.TipoHabitacionId == detalle.TipoHabitacionId &&
                                h.TipoHabitacion.MaximaOcupacion >= capacidadRequerida)
                    .ToListAsync();

                    var habitacionesOcupadas = await _context.DetalleReserva
                        .Where(d => d.Activo &&
                                    d.HabitacionId != null &&
                                    d.Reserva.Activo &&
                                    (
                                        (reserva.FechaIngreso >= d.Reserva.FechaIngreso && reserva.FechaIngreso < d.Reserva.FechaSalida) ||
                                        (reserva.FechaSalida > d.Reserva.FechaIngreso && reserva.FechaSalida <= d.Reserva.FechaSalida) ||
                                        (reserva.FechaIngreso <= d.Reserva.FechaIngreso && reserva.FechaSalida >= d.Reserva.FechaSalida)
                                    ))
                        .Select(d => d.HabitacionId.Value)
                        .ToListAsync();

                    disponibles = habitaciones
                        .Where(h => !habitacionesOcupadas.Contains(h.Id))
                        .ToList();
                }
                else if (detalle.HabitacionId != null)
                {
                    var habitacion = await _context.Habitacion
                        .FirstOrDefaultAsync(h => h.Id == detalle.HabitacionId);

                    if (habitacion != null)
                        disponibles.Add(habitacion);
                }

                habitacionesDisponibles.AddRange(disponibles);
            }

            if (!habitacionesDisponibles.Any())
            {
                return NotFound("No se encontraron habitaciones disponibles para esta reserva");
            }

            // Quitar duplicados si hay habitaciones repetidas en varios detalles
            var habitacionesUnicas = habitacionesDisponibles
                .GroupBy(h => h.Id)
                .Select(g => g.First())
                .ToList();

            // Usar ToDTO desde HabitacionesController
            var habitacionesDTO = habitacionesUnicas
                .Select(h => HabitacionsController.ToDTO(h))
                .ToList();

                return Ok(habitacionesDTO);
        }


        public static ReservaDTO ToDTO(Reserva re)
        {
            return new ReservaDTO
            {
                Id = re.Id,
                ClienteId = re.ClienteId,
                NombreCliente = re.Cliente != null ? re.Cliente?.Nombre + " " + re.Cliente?.Apellido : " ",
                Codigo = re.Codigo,
                FechaIngreso = re.FechaIngreso,
                FechaSalida = re.FechaSalida,
                LlegadaEstimada = re.LlegadaEstimada,
                Comentarios = re.Comentarios,
                EstadoId = re.EstadoId,
                Detalles = re.Detalles?.Select(d => new DetalleReservaDTO
                {
                    Id = d.Id,
                    ReservaId = d.ReservaId,
                    HabitacionId = d.HabitacionId,
                    TipoHabitacionId = d.TipoHabitacionId,
                    TipoHabitacion = d.TipoHabitacion?.Nombre,
                    NumeroHabitacion = d.Habitacion?.NumeroHabitacion,
                    CantidadAdultos = d.CantidadAdultos,
                    CantidadNinhos = d.CantidadNinhos,
                    PensionId = d.PensionId,
                    Activo = d.Activo
                }).ToList()
            };
        }

        private static string GetPensionString(int pensionId)
        {
            return pensionId switch
            {
                1 => "Sin Pensión",
                2 => "Desayuno",
                3 => "Media Pensión",
                4 => "Pensión Completa",
                _ => "Desconocida"
            };
        }

        private static string GenerarCuerpoCorreo(ReservaDTO reserva, string nombreCliente, string codigo)
        {
            var detallesHtml = string.Join("", reserva.Detalles.Select(d =>
                        $@"
                <tr>
                    <td style='padding: 8px; border: 1px solid #ccc;'>{d.CantidadAdultos}</td>
                    <td style='padding: 8px; border: 1px solid #ccc;'>{d.CantidadNinhos}</td>
                    <td style='padding: 8px; border: 1px solid #ccc;'>{GetPensionString(d.PensionId)}</td>
                </tr>"
                    ));

                    return $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: #2c3e50;'>Confirmación de Reserva</h2>
                <p>Estimado/a <strong>{nombreCliente}</strong>,</p>
                <p>Gracias por reservar con <strong>Hotel Los Álamos</strong>. A continuación se detallan los datos de su reserva:</p>

                <ul>
                    <li><strong>Código de reserva:</strong> {codigo}</li>
                    <li><strong>Fecha de ingreso:</strong> {reserva.FechaIngreso:dd/MM/yyyy}</li>
                    <li><strong>Fecha de salida:</strong> {reserva.FechaSalida:dd/MM/yyyy}</li>
                    <li><strong>Llegada estimada:</strong> {reserva.LlegadaEstimada}</li>
                    <li><strong>Comentarios:</strong> {reserva.Comentarios}</li>
                </ul>

                <h3>Detalles de la(s) habitación(es):</h3>
                <table style='border-collapse: collapse; width: 100%;'>
                    <thead>
                        <tr style='background-color: #f2f2f2;'>
                            <th style='padding: 8px; border: 1px solid #ccc;'>Adultos</th>
                            <th style='padding: 8px; border: 1px solid #ccc;'>Niños</th>
                            <th style='padding: 8px; border: 1px solid #ccc;'>Pensión</th>
                        </tr>
                        </tr>
                    </thead>
                    <tbody>
                        {detallesHtml}
                    </tbody>
                </table>
                <p>Si desea cancelar su reserva, puede acceder a ella usando <a href=""https://tusitio.com/reservas/cancelar/{reserva.Codigo}"">este enlace</a>.</p>
                <p style='margin-top: 20px;'>Nos encantará recibirle pronto. Si tiene alguna duda o desea modificar su reserva, no dude en contactarnos.</p>

                <p>Saludos cordiales,<br><strong>Hotel Los Álamos</strong></p>
            </body>
            </html>";
        }


        private static void EnviarEmailConfirmacion(ReservaDTO res, string nombreCliente, string emailDestino, string codigoReserva)
        {
            try
            {
                var fromAddress = new MailAddress("hotellosalamospy@gmail.com", "Hotel Los Alamos");

                //var toAddress = new MailAddress(emailDestino);
                var toAddress = new MailAddress(emailDestino);
                const string fromPassword = "qnacddvmoiwxpfkl";

                string subject = "Confirmación de Reserva";
            
                string body = GenerarCuerpoCorreo(res, nombreCliente, codigoReserva);

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando email: " + ex.Message);
                // Aquí puedes agregar logging o manejar la excepción como prefieras
            }
        }
    }
}

    


// qnac ddvm oiwx pfkl