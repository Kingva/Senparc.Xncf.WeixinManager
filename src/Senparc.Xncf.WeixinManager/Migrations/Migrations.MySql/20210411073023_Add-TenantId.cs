﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.WeixinManager.Migrations.Migrations.MySql
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WeixinManager_WeixinUser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WeixinManager_UserTag_WeixinUser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WeixinManager_UserTag",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WeixinManager_MpAccount",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WeixinManager_WeixinUser");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WeixinManager_UserTag_WeixinUser");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WeixinManager_UserTag");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WeixinManager_MpAccount");
        }
    }
}
