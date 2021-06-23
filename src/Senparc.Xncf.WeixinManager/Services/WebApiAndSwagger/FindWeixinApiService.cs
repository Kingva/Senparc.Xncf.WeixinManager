﻿using Senparc.CO2NET.Extensions;
using Senparc.NeuChar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Services.WebApiAndSwagger
{
    public class FindWeixinApiService
    {
        /// <summary>
        /// 微信 API 文档记录
        /// </summary>
        public static List<ApiItem> ApiItemList { get; set; } = new List<ApiItem>();

        /// <summary>
        /// 添加 API 记录
        /// </summary>
        /// <param name="platformType"></param>
        /// <param name="fullMethodName"></param>
        /// <param name="paramsPart"></param>
        /// <param name="summary"></param>
        /// <param name="isAsync"></param>
        /// <returns></returns>
        public ApiItem RecordApiItem(PlatformType platformType, string fullMethodName, string paramsPart, string summary, bool? isAsync)
        {
            if (string.IsNullOrWhiteSpace(fullMethodName))
            {
                throw new ArgumentException($"“{nameof(fullMethodName)}”不能为 null 或空白。", nameof(fullMethodName));
            }

            if (string.IsNullOrWhiteSpace(paramsPart))
            {
                throw new ArgumentException($"“{nameof(paramsPart)}”不能为 null 或空白。", nameof(paramsPart));
            }

            var apiItem = new ApiItem(platformType, fullMethodName, paramsPart, summary, isAsync);
            ApiItemList.Add(apiItem);
            return apiItem;
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        /// <param name="platformType"></param>
        /// <param name="isAsync"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public FindWeixinApiResult FindWeixinApiResult(PlatformType? platformType, bool? isAsync, string keyword)
        {
            Func<ApiItem, bool> where = z =>
                (platformType.HasValue ? z.PlatformType == platformType.Value : true) &&
                (isAsync.HasValue ? z.IsAsync == isAsync.Value : true) &&

                ((z.FullMethodName != null && z.FullMethodName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (z.Summary != null && z.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            var apis = ApiItemList.Where(where).Take(10).ToList();
            var result = new FindWeixinApiResult(platformType, isAsync, keyword, apis);
            return result;
        }
    }
}
