using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectChapterAndSubjectLecturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChapterNumber = table.Column<int>(type: "integer", nullable: false),
                    ChapterTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chapters_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chapters_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectLecturers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    LecturerId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectLecturers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectLecturers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectLecturers_Users_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "CreatedByUserId", "CreatedDate", "Description", "IsDeleted", "SubjectCode", "SubjectName", "UpdatedByUserId", "UpdatedDate" },
                values: new object[] { 1, 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This course provides knowledge and skills in developing cross-platform applications using .NET technologies.", false, "PRN222", "Advanced Cross-Platform Application Programming With .NET", null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$p7e/dWqp3/H5V2hA8gfj4egjrUGPAPfbZqBqMcSvnBcc/Qc8qpjcq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$.V0CPaW.aVn4ajd6qwur7eF84ysDgtwM6iTNNiVTUaC77F2nMaNji");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$9Eg5STUA/KUfGzB3ubcC0OGv7Mph4h14Lj3lSBPgznmpJ4Sh73oAi");

            migrationBuilder.InsertData(
                table: "Chapters",
                columns: new[] { "Id", "ChapterNumber", "ChapterTitle", "CreatedByUserId", "CreatedDate", "Description", "IsDeleted", "SubjectId", "UpdatedByUserId", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, "Networking Programming", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Introduction to network programming concepts and protocols", false, 1, null, null },
                    { 2, 2, "Asynchronous and Parallel Programming in .NET", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Understanding async/await patterns and parallel processing", false, 1, null, null },
                    { 3, 3, "Dependency Injection in .NET", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Implementing DI patterns and IoC containers in .NET applications", false, 1, null, null },
                    { 4, 4, "Building Web Application using ASP.NET Core MVC", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Creating MVC web applications with ASP.NET Core", false, 1, null, null },
                    { 5, 5, "Building Websites Using ASP.NET Core Razor Pages", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Developing page-based web applications with Razor Pages", false, 1, null, null },
                    { 6, 6, "Building a Web App with Blazor and ASP .Net Core", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Creating interactive web UIs using Blazor framework", false, 1, null, null },
                    { 7, 7, "Real-Time Communication", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Implementing real-time features with SignalR", false, 1, null, null },
                    { 8, 8, "Background Tasks with Worker Service", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Creating and managing background services in .NET", false, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "SubjectLecturers",
                columns: new[] { "Id", "AssignedDate", "LecturerId", "SubjectId" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CreatedByUserId",
                table: "Chapters",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId_ChapterNumber",
                table: "Chapters",
                columns: new[] { "SubjectId", "ChapterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_UpdatedByUserId",
                table: "Chapters",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectLecturers_LecturerId",
                table: "SubjectLecturers",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectLecturers_SubjectId_LecturerId",
                table: "SubjectLecturers",
                columns: new[] { "SubjectId", "LecturerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CreatedByUserId",
                table: "Subjects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects",
                column: "SubjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_UpdatedByUserId",
                table: "Subjects",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "SubjectLecturers");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$OwLqtBoM/jrBqZC0XGu2V.8awvRQr1uQ.AYCeduJhzJtI8IKSuN7q");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$MvIac4dYuCMb1G9EYXBX.OlhVCDEoXJLoaH3.xNEY2i7AZ5TuAVhC");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$0aOeTRYH4DD/fCfjU6a7luwhaMZa/ODrA/AMHisOpgdfy.yMpoZLG");
        }
    }
}
