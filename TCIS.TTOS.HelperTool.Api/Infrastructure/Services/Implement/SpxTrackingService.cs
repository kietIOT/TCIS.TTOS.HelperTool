using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.Dal.Enums;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public class SpxTrackingService(
        IServiceScopeFactory scopeFactory,
        ISpxExpressService spxExpressService,
        IOptions<SpxTrackingOptions> trackingOptions,
        ILogger<SpxTrackingService> logger) : ISpxTrackingService
    {
        private readonly SpxTrackingOptions _options = trackingOptions.Value;

        public async Task<BaseResponse<object>> SubscribeAsync(string spxTn, CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var existing = await uow.TrackingShipmentRepository.FindOneAsync(x => x.SpxTn == spxTn);
            if (existing != null)
            {
                if (existing.IsTerminal)
                {
                    existing.IsTerminal = false;
                    existing.NextPollAt = DateTimeOffset.UtcNow;
                    existing.PollFailCount = 0;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await uow.TrackingShipmentRepository.UpdateAsync(existing);
                    await uow.CompleteAsync();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[TRACKING] Re-activated tracking for {spxTn}");
                    Console.ResetColor();
                }

                return new BaseResponse<object>
                {
                    IsSuccess = true,
                    Message = $"Already tracking {spxTn}",
                    Data = new { spxTn, existing.Status, existing.LastEventCode }
                };
            }

            var shipment = new TrackingShipment
            {
                SpxTn = spxTn,
                Carrier = "SPX_VN",
                Status = TrackingStatus.Preparing,
                NextPollAt = DateTimeOffset.UtcNow,
                PollIntervalSec = _options.PollIntervalSeconds,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await uow.TrackingShipmentRepository.AddAsync(shipment);
            await uow.CompleteAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[TRACKING] ✅ Subscribed to track: {spxTn}");
            Console.ResetColor();

            // Immediately do first poll
            await PollSingleShipmentAsync(spxTn, ct);

            return new BaseResponse<object>
            {
                IsSuccess = true,
                Message = $"Now tracking {spxTn}",
                Data = new { spxTn }
            };
        }

        public async Task<BaseResponse<object>> UnsubscribeAsync(string spxTn, CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var existing = await uow.TrackingShipmentRepository.FindOneAsync(x => x.SpxTn == spxTn);
            if (existing == null)
            {
                return new BaseResponse<object>
                {
                    IsSuccess = false,
                    Message = $"Shipment {spxTn} not found"
                };
            }

            existing.IsTerminal = true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await uow.TrackingShipmentRepository.UpdateAsync(existing);
            await uow.CompleteAsync();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[TRACKING] ❌ Unsubscribed from: {spxTn}");
            Console.ResetColor();

            return new BaseResponse<object>
            {
                IsSuccess = true,
                Message = $"Stopped tracking {spxTn}"
            };
        }

        public async Task<BaseResponse<object>> GetShipmentStatusAsync(string spxTn, CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var shipment = await uow.TrackingShipmentRepository.FindOneAsync(x => x.SpxTn == spxTn);
            if (shipment == null)
            {
                return new BaseResponse<object>
                {
                    IsSuccess = false,
                    Message = $"Shipment {spxTn} not found"
                };
            }

            var events = await uow.TrackingEventGroupRepository.FindAsync(
                x => x.SpxTn == spxTn,
                q => q.OrderByDescending(e => e.EventTime));

            return new BaseResponse<object>
            {
                IsSuccess = true,
                Data = new
                {
                    shipment.SpxTn,
                    shipment.ClientOrderId,
                    Status = shipment.Status.ToString(),
                    shipment.LastEventCode,
                    shipment.LastMilestoneName,
                    shipment.LastMessage,
                    LastEventTime = shipment.LastEventTime?.ToString("dd/MM/yyyy HH:mm:ss"),
                    shipment.CurrentLocationName,
                    shipment.CurrentFullAddress,
                    shipment.NextLocationName,
                    shipment.NextFullAddress,
                    shipment.IsTerminal,
                    Events = events.Select(e => new
                    {
                        e.TrackingCode,
                        e.MilestoneName,
                        e.BuyerMessage,
                        e.Description,
                        EventTime = e.EventTime.ToString("dd/MM/yyyy HH:mm:ss")
                    })
                },
                Message = "Shipment status"
            };
        }

        public async Task<BaseResponse<object>> GetAllActiveShipmentsAsync(CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var shipments = await uow.TrackingShipmentRepository.FindAsync(x => !x.IsTerminal);

            return new BaseResponse<object>
            {
                IsSuccess = true,
                Data = shipments.Select(s => new
                {
                    s.SpxTn,
                    s.ClientOrderId,
                    Status = s.Status.ToString(),
                    s.LastEventCode,
                    s.LastMilestoneName,
                    s.LastMessage,
                    LastEventTime = s.LastEventTime?.ToString("dd/MM/yyyy HH:mm:ss"),
                    s.CurrentLocationName,
                    s.IsTerminal
                }),
                Message = "Active shipments"
            };
        }

        public async Task PollAndUpdateAsync(CancellationToken ct = default)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var now = DateTimeOffset.UtcNow;
            var dueShipments = await uow.TrackingShipmentRepository.FindAsync(
                x => !x.IsTerminal && x.NextPollAt <= now && x.PollFailCount < _options.MaxPollFailCount);

            var shipmentList = dueShipments.ToList();
            if (shipmentList.Count == 0) return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[TRACKING] 🔄 Polling {shipmentList.Count} shipment(s)...");
            Console.ResetColor();

            foreach (var shipment in shipmentList)
            {
                try
                {
                    await PollSingleShipmentAsync(shipment.SpxTn, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Poll failed for {SpxTn}", shipment.SpxTn);
                }

                if (ct.IsCancellationRequested) break;
            }
        }

        private async Task PollSingleShipmentAsync(string spxTn, CancellationToken ct)
        {
            var apiResult = await spxExpressService.GetItemByOrderIdAsync(spxTn);

            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

            var shipment = await uow.TrackingShipmentRepository.FindOneAsync(x => x.SpxTn == spxTn);
            if (shipment == null) return;

            shipment.LastPolledAt = DateTimeOffset.UtcNow;

            if (!apiResult.IsSuccess || apiResult.Data == null)
            {
                shipment.PollFailCount++;
                shipment.NextPollAt = DateTimeOffset.UtcNow.AddSeconds(shipment.PollIntervalSec * 2);
                shipment.UpdatedAt = DateTimeOffset.UtcNow;
                await uow.TrackingShipmentRepository.UpdateAsync(shipment);
                await uow.CompleteAsync();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[TRACKING] ⚠️  Poll failed for {spxTn}: {apiResult.Message}");
                Console.ResetColor();
                return;
            }

            // Parse the API response
            var json = JsonConvert.SerializeObject(apiResult.Data);
            var trackingData = JsonConvert.DeserializeObject<SpxTrackingData>(json);

            if (trackingData?.SlsTrackingInfo?.Records == null || trackingData.SlsTrackingInfo.Records.Count == 0)
            {
                shipment.PollFailCount++;
                shipment.NextPollAt = DateTimeOffset.UtcNow.AddSeconds(shipment.PollIntervalSec);
                shipment.UpdatedAt = DateTimeOffset.UtcNow;
                await uow.TrackingShipmentRepository.UpdateAsync(shipment);
                await uow.CompleteAsync();
                return;
            }

            // Compute fingerprint to detect changes
            var recordsJson = JsonConvert.SerializeObject(trackingData.SlsTrackingInfo.Records);
            var newFingerprint = ComputeSha256(recordsJson);

            if (string.Equals(shipment.Fingerprint, newFingerprint, StringComparison.Ordinal))
            {
                // No change
                shipment.PollFailCount = 0;
                shipment.NextPollAt = DateTimeOffset.UtcNow.AddSeconds(shipment.PollIntervalSec);
                shipment.UpdatedAt = DateTimeOffset.UtcNow;
                await uow.TrackingShipmentRepository.UpdateAsync(shipment);
                await uow.CompleteAsync();
                return;
            }

            // --- NEW EVENTS DETECTED ---
            var records = trackingData.SlsTrackingInfo.Records;
            var latestRecord = records.First();

            // Update shipment info
            shipment.ClientOrderId = trackingData.SlsTrackingInfo.ClientOrderId;
            shipment.DeliverType = trackingData.FulfillmentInfo?.DeliverType;
            shipment.LastEventCode = latestRecord.TrackingCode;
            shipment.LastMessage = latestRecord.BuyerDescription ?? latestRecord.Description;
            shipment.LastMilestoneName = latestRecord.MilestoneName;
            shipment.LastEventTime = DateTimeOffset.FromUnixTimeSeconds(latestRecord.ActualTime);
            shipment.Fingerprint = newFingerprint;
            shipment.LastRawJson = json;
            shipment.PollFailCount = 0;
            shipment.UpdatedAt = DateTimeOffset.UtcNow;

            // Location info
            if (latestRecord.CurrentLocation != null)
            {
                shipment.CurrentLocationName = latestRecord.CurrentLocation.LocationName;
                shipment.CurrentFullAddress = latestRecord.CurrentLocation.FullAddress;
                shipment.CurrentLat = double.TryParse(latestRecord.CurrentLocation.LatRaw, out var lat) ? lat : null;
                shipment.CurrentLng = double.TryParse(latestRecord.CurrentLocation.LngRaw, out var lng) ? lng : null;
            }

            if (latestRecord.NextLocation != null)
            {
                shipment.NextLocationName = latestRecord.NextLocation.LocationName;
                shipment.NextFullAddress = latestRecord.NextLocation.FullAddress;
                shipment.NextLat = double.TryParse(latestRecord.NextLocation.LatRaw, out var nlat) ? nlat : null;
                shipment.NextLng = double.TryParse(latestRecord.NextLocation.LngRaw, out var nlng) ? nlng : null;
            }

            // Map status
            var previousStatus = shipment.Status;
            shipment.Status = MapTrackingStatus(latestRecord.TrackingCode, latestRecord.MilestoneCode);

            // Check if terminal (Delivered, Cancelled, Returned)
            var isTerminal = shipment.Status is TrackingStatus.Delivered
                or TrackingStatus.Cancelled or TrackingStatus.Returned;
            shipment.IsTerminal = isTerminal;
            shipment.NextPollAt = isTerminal
                ? DateTimeOffset.MaxValue
                : DateTimeOffset.UtcNow.AddSeconds(shipment.PollIntervalSec);

            await uow.TrackingShipmentRepository.UpdateAsync(shipment);

            // Save new events
            var existingEvents = await uow.TrackingEventGroupRepository.FindAsync(x => x.SpxTn == spxTn);
            var existingKeys = existingEvents
                .Select(e => $"{e.SpxTn}|{e.EventTime.ToUnixTimeSeconds()}|{e.TrackingCode}")
                .ToHashSet();

            var newEvents = new List<TrackingEvent>();
            foreach (var record in records)
            {
                var eventTime = DateTimeOffset.FromUnixTimeSeconds(record.ActualTime);
                var key = $"{spxTn}|{record.ActualTime}|{record.TrackingCode}";

                if (existingKeys.Contains(key)) continue;

                newEvents.Add(new TrackingEvent
                {
                    SpxTn = spxTn,
                    EventTime = eventTime,
                    TrackingCode = record.TrackingCode ?? "UNKNOWN",
                    MilestoneCode = record.MilestoneCode,
                    MilestoneName = record.MilestoneName,
                    BuyerMessage = record.BuyerDescription,
                    SellerMessage = record.SellerDescription,
                    Description = record.Description,
                    CurrentLocation = record.CurrentLocation != null ? JsonConvert.SerializeObject(record.CurrentLocation) : null,
                    NextLocation = record.NextLocation != null ? JsonConvert.SerializeObject(record.NextLocation) : null,
                    RawRecord = JsonConvert.SerializeObject(record),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            if (newEvents.Count > 0)
            {
                await uow.TrackingEventGroupRepository.AddRangeAsync(newEvents);
            }

            await uow.CompleteAsync();

            // Console output
            PrintStatusUpdate(shipment, previousStatus, newEvents);
        }

        private static void PrintStatusUpdate(TrackingShipment shipment, TrackingStatus previousStatus, List<TrackingEvent> newEvents)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  📦 SHIPMENT UPDATE: {shipment.SpxTn,-39}║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");

            if (previousStatus != shipment.Status)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"║  Status: {previousStatus} → {shipment.Status,-40}║");
            }
            else
            {
                Console.WriteLine($"║  Status: {shipment.Status,-51}║");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"║  Milestone: {shipment.LastMilestoneName,-48}║");

            if (!string.IsNullOrEmpty(shipment.LastMessage))
            {
                var msg = shipment.LastMessage.Length > 55
                    ? shipment.LastMessage[..55] + "..."
                    : shipment.LastMessage;
                Console.WriteLine($"║  Message: {msg,-50}║");
            }

            if (!string.IsNullOrEmpty(shipment.CurrentLocationName))
            {
                Console.WriteLine($"║  📍 Location: {shipment.CurrentLocationName,-46}║");
            }

            if (!string.IsNullOrEmpty(shipment.NextLocationName))
            {
                Console.WriteLine($"║  ➡️  Next: {shipment.NextLocationName,-49}║");
            }

            if (shipment.LastEventTime.HasValue)
            {
                var localTime = shipment.LastEventTime.Value.ToOffset(TimeSpan.FromHours(7));
                Console.WriteLine($"║  🕐 Time: {localTime:dd/MM/yyyy HH:mm:ss,-49}║");
            }

            if (newEvents.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╠──────────────────────────────────────────────────────────────╣");
                Console.WriteLine($"║  📋 {newEvents.Count} new event(s):                                          ║");

                foreach (var evt in newEvents.OrderBy(e => e.EventTime))
                {
                    var time = evt.EventTime.ToOffset(TimeSpan.FromHours(7));
                    var desc = evt.BuyerMessage ?? evt.Description ?? "";
                    if (desc.Length > 45) desc = desc[..45] + "...";

                    Console.ForegroundColor = GetEventColor(evt.MilestoneCode);
                    Console.WriteLine($"║    [{time:HH:mm}] {desc,-54}║");
                }
            }

            Console.ForegroundColor = ConsoleColor.White;

            if (shipment.IsTerminal)
            {
                Console.ForegroundColor = shipment.Status == TrackingStatus.Delivered
                    ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
                Console.WriteLine($"║  🏁 TRACKING COMPLETED — {shipment.Status,-35}║");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static ConsoleColor GetEventColor(int? milestoneCode) => milestoneCode switch
        {
            1 => ConsoleColor.Gray,      // Preparing to ship
            5 => ConsoleColor.Cyan,      // In transit
            6 => ConsoleColor.Yellow,    // Out for delivery
            8 => ConsoleColor.Green,     // Delivered
            _ => ConsoleColor.White
        };

        private static TrackingStatus MapTrackingStatus(string? trackingCode, int milestoneCode)
        {
            // Terminal statuses by tracking code
            if (trackingCode != null)
            {
                if (trackingCode.StartsWith("F98") || trackingCode == "F980")
                    return TrackingStatus.Delivered;

                if (trackingCode.StartsWith("F9") && trackingCode != "F980")
                {
                    if (trackingCode is "F910" or "F920" or "F930" or "F940")
                        return TrackingStatus.Exception;
                }
            }

            return milestoneCode switch
            {
                1 => TrackingStatus.Preparing,        // Preparing to ship
                5 => TrackingStatus.InTransit,         // In transit
                6 => TrackingStatus.OutForDelivery,    // Out for delivery
                8 => TrackingStatus.Delivered,         // Delivered
                9 => TrackingStatus.Returned,          // Returned
                10 => TrackingStatus.Cancelled,        // Cancelled
                _ => TrackingStatus.InTransit
            };
        }

        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
