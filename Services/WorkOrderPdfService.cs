using IT15_Project.Data;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace IT15_Project.Services
{
    /// <summary>
    /// Service for generating professional Work Order PDF reports
    /// Uses iTextSharp for clean, enterprise-style document generation
    /// </summary>
    public class WorkOrderPdfService : IWorkOrderPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkOrderPdfService> _logger;

        // PDF styling constants
        private static readonly BaseColor HeaderColor = new BaseColor(25, 118, 210); // Blue
        private static readonly BaseColor LightGray = new BaseColor(245, 245, 245);
        private static readonly BaseColor DarkGray = new BaseColor(97, 97, 97);
        private static readonly BaseColor BorderColor = new BaseColor(224, 224, 224);

        public WorkOrderPdfService(
            ApplicationDbContext context,
            ILogger<WorkOrderPdfService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GenerateWorkOrderPdfAsync(int workOrderId, int companyId)
        {
            try
            {
                // Load work order with all related data
                var workOrder = await _context.WorkOrders
                    .Include(wo => wo.Asset)
                    .Include(wo => wo.AssignedToPersonnel)
                    .Include(wo => wo.CreatedByPersonnel)
                    .Include(wo => wo.Company)
                    .Include(wo => wo.MaintenanceRequest)
                    .Include(wo => wo.PreventiveSchedule)
                    .Where(wo => wo.WorkOrderId == workOrderId && wo.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (workOrder == null)
                {
                    throw new InvalidOperationException($"Work Order {workOrderId} not found or access denied.");
                }

                // Load parts used
                var partsUsed = await _context.WorkOrderParts
                    .Include(wop => wop.Part)
                    .Where(wop => wop.WorkOrderId == workOrderId && wop.CompanyId == companyId)
                    .ToListAsync();

                // Load cost breakdown
                var cost = await _context.WorkOrderCosts
                    .Where(woc => woc.WorkOrderId == workOrderId && woc.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                // Load maintenance log (completion notes)
                var maintenanceLog = await _context.MaintenanceLogs
                    .Include(ml => ml.CompletedByPersonnel)
                    .Where(ml => ml.WorkOrderId == workOrderId && ml.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                // Generate PDF using iTextSharp
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4, 40, 40, 40, 40);
                    var writer = PdfWriter.GetInstance(document, memoryStream);
                    
                    document.Open();
                    
                    // Add content
                    AddHeader(document, workOrder);
                    AddWorkOrderInfo(document, workOrder);
                    AddAssetInfo(document, workOrder);
                    AddPersonnelInfo(document, workOrder);
                    AddMaintenanceDetails(document, workOrder, maintenanceLog);
                    AddPartsUsed(document, partsUsed);
                    AddLaborCost(document, workOrder, cost);
                    AddCostSummary(document, cost, workOrder, maintenanceLog);
                    AddSignatures(document, workOrder);
                    AddFooter(document);
                    
                    document.Close();
                    
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for Work Order {WorkOrderId}", workOrderId);
                throw;
            }
        }

        private void AddHeader(Document document, Models.WorkOrder workOrder)
        {
            var headerTable = new PdfPTable(2) { WidthPercentage = 100 };
            headerTable.SetWidths(new float[] { 70, 30 });

            // Left side - Company info
            var leftCell = new PdfPCell();
            leftCell.Border = Rectangle.NO_BORDER;
            leftCell.BorderWidthBottom = 2;
            leftCell.BorderColorBottom = HeaderColor;
            leftCell.PaddingBottom = 10;

            var companyFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, HeaderColor);
            var companyPara = new Paragraph(workOrder.Company?.CompanyName ?? "MaintenX", companyFont);
            leftCell.AddElement(companyPara);

            if (!string.IsNullOrEmpty(workOrder.Company?.Address))
            {
                var addressFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray);
                leftCell.AddElement(new Paragraph(workOrder.Company.Address, addressFont));
            }

            if (!string.IsNullOrEmpty(workOrder.Company?.ContactEmail))
            {
                var emailFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray);
                leftCell.AddElement(new Paragraph(workOrder.Company.ContactEmail, emailFont));
            }

            headerTable.AddCell(leftCell);

            // Right side - Work Order number
            var rightCell = new PdfPCell();
            rightCell.Border = Rectangle.NO_BORDER;
            rightCell.BorderWidthBottom = 2;
            rightCell.BorderColorBottom = HeaderColor;
            rightCell.PaddingBottom = 10;
            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;

            var woFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, HeaderColor);
            rightCell.AddElement(new Paragraph("WORK ORDER", woFont));

            var woNumFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, DarkGray);
            rightCell.AddElement(new Paragraph($"#WO-{workOrder.WorkOrderId:D4}", woNumFont));

            headerTable.AddCell(rightCell);

            document.Add(headerTable);
            document.Add(new Paragraph(" ") { SpacingAfter = 10 });
        }

        private void AddWorkOrderInfo(Document document, Models.WorkOrder workOrder)
        {
            AddSectionHeader(document, "Work Order Information");

            var table = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 15 };
            table.SetWidths(new float[] { 25, 25, 25, 25 });

            AddInfoRow(table, "Status:", workOrder.Status ?? "-", "Priority:", workOrder.Priority ?? "-");
            AddInfoRow(table, "Source:", GetWorkOrderSource(workOrder), "Created Date:", workOrder.DateCreated?.ToString("MMM dd, yyyy") ?? "-");
            AddInfoRow(table, "Due Date:", workOrder.DueDate?.ToString("MMM dd, yyyy") ?? "-", "Completion Date:", workOrder.ActualCompletion?.ToString("MMM dd, yyyy") ?? "-");

            document.Add(table);
        }

        private void AddAssetInfo(Document document, Models.WorkOrder workOrder)
        {
            AddSectionHeader(document, "Asset Information");

            var table = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 15 };
            table.SetWidths(new float[] { 25, 25, 25, 25 });

            AddInfoRow(table, "Asset Name:", workOrder.Asset?.AssetName ?? "N/A", "Asset Code:", workOrder.Asset?.AssetCode ?? "-");
            AddInfoRow(table, "Location:", workOrder.Asset?.Location ?? "-", "Asset Status:", workOrder.Asset?.Status ?? "-");

            document.Add(table);
        }

        private void AddPersonnelInfo(Document document, Models.WorkOrder workOrder)
        {
            AddSectionHeader(document, "Assigned Personnel");

            var table = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 15 };
            table.SetWidths(new float[] { 25, 25, 25, 25 });

            AddInfoRow(table, "Assigned Technician:", workOrder.AssignedToPersonnel?.FullName ?? "Unassigned", 
                       "Created By:", workOrder.CreatedByPersonnel?.FullName ?? "-");

            document.Add(table);
        }

        private void AddMaintenanceDetails(Document document, Models.WorkOrder workOrder, Models.MaintenanceLog? maintenanceLog)
        {
            AddSectionHeader(document, "Maintenance Details");

            var detailsTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
            var cell = new PdfPCell();
            cell.BackgroundColor = LightGray;
            cell.Padding = 10;
            cell.Border = Rectangle.BOX;
            cell.BorderColor = BorderColor;

            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            cell.AddElement(new Paragraph("Issue Description:", boldFont));
            cell.AddElement(new Paragraph(workOrder.Description ?? "No description provided.", normalFont) { SpacingBefore = 5 });

            if (maintenanceLog != null && !string.IsNullOrEmpty(maintenanceLog.Description))
            {
                cell.AddElement(new Paragraph("Work Performed:", boldFont) { SpacingBefore = 10 });
                cell.AddElement(new Paragraph(maintenanceLog.Description, normalFont) { SpacingBefore = 5 });
            }

            if (maintenanceLog != null && !string.IsNullOrEmpty(maintenanceLog.Notes))
            {
                cell.AddElement(new Paragraph("Completion Notes:", boldFont) { SpacingBefore = 10 });
                cell.AddElement(new Paragraph(maintenanceLog.Notes, normalFont) { SpacingBefore = 5 });
            }

            detailsTable.AddCell(cell);
            document.Add(detailsTable);
        }

        private void AddPartsUsed(Document document, List<Models.WorkOrderPart> partsUsed)
        {
            AddSectionHeader(document, "Parts Used & Inventory Consumption");

            if (partsUsed.Any())
            {
                var table = new PdfPTable(5) { WidthPercentage = 100, SpacingAfter = 15 };
                table.SetWidths(new float[] { 30, 20, 10, 15, 15 });

                // Header
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                AddTableHeader(table, "Part Name", headerFont);
                AddTableHeader(table, "Part Number", headerFont);
                AddTableHeader(table, "Qty", headerFont, Element.ALIGN_RIGHT);
                AddTableHeader(table, "Unit Cost", headerFont, Element.ALIGN_RIGHT);
                AddTableHeader(table, "Total Cost", headerFont, Element.ALIGN_RIGHT);

                // Rows
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var grayFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray);
                
                foreach (var part in partsUsed)
                {
                    AddTableCell(table, part.Part?.PartName ?? "Unknown Part", cellFont);
                    AddTableCell(table, part.Part?.PartNumber ?? "-", grayFont);
                    AddTableCell(table, part.QuantityUsed.ToString(), cellFont, Element.ALIGN_RIGHT);
                    AddTableCell(table, $"₱ {part.UnitCost:N2}", cellFont, Element.ALIGN_RIGHT);
                    AddTableCell(table, $"₱ {part.TotalCost:N2}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9), Element.ALIGN_RIGHT);
                }

                // Subtotal
                var totalPartsCost = partsUsed.Sum(p => p.TotalCost ?? 0);
                var footerCell = new PdfPCell(new Phrase("Subtotal - Parts Cost:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                footerCell.Colspan = 4;
                footerCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                footerCell.BackgroundColor = LightGray;
                footerCell.Padding = 5;
                footerCell.BorderWidth = 2;
                footerCell.BorderColor = DarkGray;
                table.AddCell(footerCell);

                var totalCell = new PdfPCell(new Phrase($"₱ {totalPartsCost:N2}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, HeaderColor)));
                totalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalCell.BackgroundColor = LightGray;
                totalCell.Padding = 5;
                totalCell.BorderWidth = 2;
                totalCell.BorderColor = DarkGray;
                table.AddCell(totalCell);

                document.Add(table);
            }
            else
            {
                var noPartsTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
                var cell = new PdfPCell(new Phrase("No parts consumed for this work order.", 
                    FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10, DarkGray)));
                cell.BackgroundColor = LightGray;
                cell.Padding = 10;
                cell.Border = Rectangle.BOX;
                cell.BorderColor = BorderColor;
                noPartsTable.AddCell(cell);
                document.Add(noPartsTable);
            }
        }

        private void AddLaborCost(Document document, Models.WorkOrder workOrder, Models.WorkOrderCost? cost)
        {
            AddSectionHeader(document, "Labor Cost Breakdown");

            var table = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
            var cell = new PdfPCell();
            cell.BackgroundColor = new BaseColor(250, 250, 250);
            cell.Padding = 12;
            cell.Border = Rectangle.BOX;
            cell.BorderColor = BorderColor;

            var labelFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, DarkGray);
            var valueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray);

            // Technician info
            var techTable = new PdfPTable(2) { WidthPercentage = 100 };
            techTable.SetWidths(new float[] { 60, 40 });
            
            var techLabelCell = new PdfPCell(new Phrase("Assigned Technician:", labelFont));
            techLabelCell.Border = Rectangle.NO_BORDER;
            techTable.AddCell(techLabelCell);
            
            var techValueCell = new PdfPCell(new Phrase(workOrder.AssignedToPersonnel?.FullName ?? "Unassigned", valueFont));
            techValueCell.Border = Rectangle.NO_BORDER;
            techValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            techTable.AddCell(techValueCell);

            if (!string.IsNullOrEmpty(workOrder.AssignedToPersonnel?.Position))
            {
                var posLabelCell = new PdfPCell(new Phrase("Position:", smallFont));
                posLabelCell.Border = Rectangle.NO_BORDER;
                techTable.AddCell(posLabelCell);
                
                var posValueCell = new PdfPCell(new Phrase(workOrder.AssignedToPersonnel.Position, smallFont));
                posValueCell.Border = Rectangle.NO_BORDER;
                posValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                techTable.AddCell(posValueCell);
            }

            if (workOrder.AssignedToPersonnel?.HourlyRate.HasValue == true && workOrder.AssignedToPersonnel.HourlyRate > 0)
            {
                var rateLabelCell = new PdfPCell(new Phrase("Hourly Rate:", smallFont));
                rateLabelCell.Border = Rectangle.NO_BORDER;
                techTable.AddCell(rateLabelCell);
                
                var rateValueCell = new PdfPCell(new Phrase($"₱ {workOrder.AssignedToPersonnel.HourlyRate:N2}/hr", smallFont));
                rateValueCell.Border = Rectangle.NO_BORDER;
                rateValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                techTable.AddCell(rateValueCell);
            }

            cell.AddElement(techTable);

            // Separator
            var separator = new Paragraph(" ") { SpacingBefore = 8, SpacingAfter = 8 };
            separator.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, BorderColor, Element.ALIGN_CENTER, -2)));
            cell.AddElement(separator);

            // Labor cost
            var laborCost = cost?.LaborCost ?? 0;
            var costTable = new PdfPTable(2) { WidthPercentage = 100 };
            costTable.SetWidths(new float[] { 60, 40 });
            
            var costLabelCell = new PdfPCell(new Phrase("Labor Cost:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11)));
            costLabelCell.Border = Rectangle.NO_BORDER;
            costTable.AddCell(costLabelCell);
            
            var costValueCell = new PdfPCell(new Phrase($"₱ {laborCost:N2}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, HeaderColor)));
            costValueCell.Border = Rectangle.NO_BORDER;
            costValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            costTable.AddCell(costValueCell);

            cell.AddElement(costTable);

            if (laborCost == 0)
            {
                cell.AddElement(new Paragraph("(No labor cost recorded)", 
                    FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, DarkGray)) { SpacingBefore = 3 });
            }

            table.AddCell(cell);
            document.Add(table);
        }

        private void AddCostSummary(Document document, Models.WorkOrderCost? cost, Models.WorkOrder workOrder, Models.MaintenanceLog? maintenanceLog)
        {
            AddSectionHeader(document, "Maintenance Cost Summary");

            var laborCost = cost?.LaborCost ?? 0;
            var partsCost = cost?.PartsCost ?? 0;
            var otherCost = cost?.OtherCost ?? 0;
            var totalCost = cost?.TotalCost ?? 0;

            var table = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
            var cell = new PdfPCell();
            cell.BackgroundColor = new BaseColor(227, 242, 253); // Light blue
            cell.Padding = 15;
            cell.Border = Rectangle.BOX;
            cell.BorderColor = BorderColor;

            // Cost breakdown
            var costTable = new PdfPTable(2) { WidthPercentage = 100 };
            costTable.SetWidths(new float[] { 60, 40 });

            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            
            AddCostRow(costTable, "Labor Cost", laborCost, normalFont);
            AddCostRow(costTable, "Parts Cost", partsCost, normalFont);
            AddCostRow(costTable, "Other Cost", otherCost, normalFont);

            // Separator
            var separatorCell = new PdfPCell();
            separatorCell.Colspan = 2;
            separatorCell.Border = Rectangle.TOP_BORDER;
            separatorCell.BorderWidth = 2;
            separatorCell.BorderColor = HeaderColor;
            separatorCell.FixedHeight = 8;
            costTable.AddCell(separatorCell);

            // Total
            var totalLabelCell = new PdfPCell(new Phrase("Total Maintenance Cost", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, HeaderColor)));
            totalLabelCell.Border = Rectangle.NO_BORDER;
            totalLabelCell.Padding = 5;
            costTable.AddCell(totalLabelCell);

            var totalValueCell = new PdfPCell(new Phrase($"₱ {totalCost:N2}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, HeaderColor)));
            totalValueCell.Border = Rectangle.NO_BORDER;
            totalValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            totalValueCell.Padding = 5;
            costTable.AddCell(totalValueCell);

            cell.AddElement(costTable);

            // Cost distribution percentages
            if (totalCost > 0)
            {
                var separator = new Paragraph(" ") { SpacingBefore = 10, SpacingAfter = 10 };
                separator.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, new BaseColor(187, 222, 251), Element.ALIGN_CENTER, -2)));
                cell.AddElement(separator);

                cell.AddElement(new Paragraph("Cost Distribution:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, DarkGray)));

                var laborPercent = totalCost > 0 ? (laborCost / totalCost * 100) : 0;
                var partsPercent = totalCost > 0 ? (partsCost / totalCost * 100) : 0;
                var otherPercent = totalCost > 0 ? (otherCost / totalCost * 100) : 0;

                var distTable = new PdfPTable(3) { WidthPercentage = 100, SpacingBefore = 5 };
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray);
                
                var laborCell = new PdfPCell(new Phrase($"Labor: {laborPercent:F1}%", smallFont));
                laborCell.Border = Rectangle.NO_BORDER;
                distTable.AddCell(laborCell);

                var partsCell = new PdfPCell(new Phrase($"Parts: {partsPercent:F1}%", smallFont));
                partsCell.Border = Rectangle.NO_BORDER;
                distTable.AddCell(partsCell);

                var otherCell = new PdfPCell(new Phrase($"Other: {otherPercent:F1}%", smallFont));
                otherCell.Border = Rectangle.NO_BORDER;
                distTable.AddCell(otherCell);

                cell.AddElement(distTable);
            }

            // Cost status indicator
            var statusTable = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 8 };
            var statusCell = new PdfPCell();
            statusCell.BackgroundColor = BaseColor.White;
            statusCell.Padding = 8;
            statusCell.Border = Rectangle.BOX;
            statusCell.BorderColor = BorderColor;

            statusCell.AddElement(new Paragraph("Cost Status:", FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray)));
            var statusColor = workOrder.Status == "Completed" ? new BaseColor(46, 125, 50) : new BaseColor(237, 108, 2);
            statusCell.AddElement(new Paragraph(workOrder.Status == "Completed" ? "Final" : "Estimated", 
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, statusColor)) { SpacingBefore = 2 });
            statusTable.AddCell(statusCell);

            if (maintenanceLog != null)
            {
                var recordedCell = new PdfPCell();
                recordedCell.BackgroundColor = BaseColor.White;
                recordedCell.Padding = 8;
                recordedCell.Border = Rectangle.BOX;
                recordedCell.BorderColor = BorderColor;

                recordedCell.AddElement(new Paragraph("Recorded By:", FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray)));
                recordedCell.AddElement(new Paragraph(maintenanceLog.CompletedByPersonnel?.FullName ?? "System", 
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)) { SpacingBefore = 2 });
                statusTable.AddCell(recordedCell);
            }
            else
            {
                var emptyCell = new PdfPCell();
                emptyCell.Border = Rectangle.NO_BORDER;
                statusTable.AddCell(emptyCell);
            }

            cell.AddElement(statusTable);

            table.AddCell(cell);
            document.Add(table);

            // Cost notes
            if (cost != null && cost.OtherCost > 0 && !string.IsNullOrEmpty(cost.Notes))
            {
                var notesTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
                var notesCell = new PdfPCell();
                notesCell.BackgroundColor = new BaseColor(250, 250, 250);
                notesCell.Padding = 10;
                notesCell.Border = Rectangle.BOX;
                notesCell.BorderColor = BorderColor;

                notesCell.AddElement(new Paragraph("Cost Notes:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, DarkGray)));
                notesCell.AddElement(new Paragraph(cost.Notes, FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray)) { SpacingBefore = 3 });

                notesTable.AddCell(notesCell);
                document.Add(notesTable);
            }
        }

        private void AddSignatures(Document document, Models.WorkOrder workOrder)
        {
            document.Add(new Paragraph(" ") { SpacingBefore = 25 });

            var table = new PdfPTable(3) { WidthPercentage = 100, SpacingAfter = 15 };
            table.SetWidths(new float[] { 33, 33, 33 });

            var labelFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, DarkGray);
            var valueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Prepared By
            var preparedCell = new PdfPCell();
            preparedCell.Border = Rectangle.NO_BORDER;
            preparedCell.BorderWidthTop = 1;
            preparedCell.BorderColorTop = DarkGray;
            preparedCell.PaddingTop = 5;
            preparedCell.AddElement(new Paragraph("Prepared By", labelFont));
            preparedCell.AddElement(new Paragraph(workOrder.CreatedByPersonnel?.FullName ?? "System", valueFont) { SpacingBefore = 2 });
            table.AddCell(preparedCell);

            // Approved By
            var approvedCell = new PdfPCell();
            approvedCell.Border = Rectangle.NO_BORDER;
            approvedCell.BorderWidthTop = 1;
            approvedCell.BorderColorTop = DarkGray;
            approvedCell.PaddingTop = 5;
            approvedCell.AddElement(new Paragraph("Approved By", labelFont));
            approvedCell.AddElement(new Paragraph("_____________________", valueFont) { SpacingBefore = 2 });
            table.AddCell(approvedCell);

            // Technician Signature
            var techCell = new PdfPCell();
            techCell.Border = Rectangle.NO_BORDER;
            techCell.BorderWidthTop = 1;
            techCell.BorderColorTop = DarkGray;
            techCell.PaddingTop = 5;
            techCell.AddElement(new Paragraph("Technician Signature", labelFont));
            techCell.AddElement(new Paragraph(workOrder.AssignedToPersonnel?.FullName ?? "Unassigned", valueFont) { SpacingBefore = 2 });
            table.AddCell(techCell);

            document.Add(table);
        }

        private void AddFooter(Document document)
        {
            var footerTable = new PdfPTable(1) { WidthPercentage = 100 };
            var cell = new PdfPCell();
            cell.Border = Rectangle.NO_BORDER;
            cell.BorderWidthTop = 1;
            cell.BorderColorTop = new BaseColor(224, 224, 224);
            cell.PaddingTop = 5;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;

            var grayFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray);
            var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(97, 97, 97));

            var para = new Paragraph();
            para.Add(new Chunk("Generated on: ", grayFont));
            para.Add(new Chunk(DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"), dateFont));
            cell.AddElement(para);

            cell.AddElement(new Paragraph("MaintenX - Maintenance Management System", grayFont) { SpacingBefore = 2 });

            footerTable.AddCell(cell);
            document.Add(footerTable);
        }

        // Helper methods
        private void AddSectionHeader(Document document, string title)
        {
            var headerTable = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 15, SpacingAfter = 10 };
            var cell = new PdfPCell(new Phrase(title, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.White)));
            cell.BackgroundColor = HeaderColor;
            cell.Padding = 8;
            cell.Border = Rectangle.NO_BORDER;
            headerTable.AddCell(cell);
            document.Add(headerTable);
        }

        private void AddInfoRow(PdfPTable table, string label1, string value1, string label2, string value2)
        {
            var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            var cell1 = new PdfPCell(new Phrase(label1, labelFont));
            cell1.Border = Rectangle.NO_BORDER;
            cell1.Padding = 5;
            table.AddCell(cell1);

            var cell2 = new PdfPCell(new Phrase(value1, valueFont));
            cell2.Border = Rectangle.NO_BORDER;
            cell2.Padding = 5;
            table.AddCell(cell2);

            var cell3 = new PdfPCell(new Phrase(label2, labelFont));
            cell3.Border = Rectangle.NO_BORDER;
            cell3.Padding = 5;
            table.AddCell(cell3);

            var cell4 = new PdfPCell(new Phrase(value2, valueFont));
            cell4.Border = Rectangle.NO_BORDER;
            cell4.Padding = 5;
            table.AddCell(cell4);
        }

        private void AddTableHeader(PdfPTable table, string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = LightGray;
            cell.Padding = 5;
            cell.BorderWidth = 1;
            cell.BorderColor = DarkGray;
            cell.HorizontalAlignment = alignment;
            table.AddCell(cell);
        }

        private void AddTableCell(PdfPTable table, string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.BorderWidth = 1;
            cell.BorderColor = BorderColor;
            cell.HorizontalAlignment = alignment;
            table.AddCell(cell);
        }

        private void AddCostRow(PdfPTable table, string label, decimal amount, Font font)
        {
            var labelCell = new PdfPCell(new Phrase(label, font));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 5;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase($"₱ {amount:N2}", font));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            valueCell.Padding = 5;
            table.AddCell(valueCell);
        }

        private string GetWorkOrderSource(Models.WorkOrder workOrder)
        {
            if (workOrder.Source == "Preventive" || workOrder.PreventiveScheduleId.HasValue)
            {
                return "Preventive Maintenance";
            }
            else if (workOrder.MaintenanceRequestId.HasValue)
            {
                return $"Maintenance Request #{workOrder.MaintenanceRequest?.RequestNumber}";
            }
            else
            {
                return "Manual";
            }
        }
    }
}
