﻿using Blazored.LocalStorage;
using BlazorTable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using PrimeView.Entities;
using PrimeView.Frontend.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PrimeView.Frontend.Pages
{
	public partial class ReportDetails
	{
		private const string FilterPresetStorageKey = "ResultFilterPresets";

		[Inject]
		public NavigationManager NavigationManager { get; set; }

		[Inject]
		public HttpClient Http { get; set; }

		[Inject]
		public ISyncLocalStorageService LocalStorage { get; set; }

		[Inject]
		public IReportReader ReportReader { get; set; }

		[Inject]
		public IJSInProcessRuntime JSRuntime { get; set; }

		[QueryStringParameter("sc")]
		public string SortColumn { get; set; } = "pp";

		[QueryStringParameter("sd")]
		public bool SortDescending { get; set; } = true;

		[QueryStringParameter("hi")]
		public bool HideSystemInformation { get; set; } = false;

		[QueryStringParameter("hf")]
		public bool HideFilters { get; set; } = false;

		[QueryStringParameter("hp")]
		public bool HideFilterPresets { get; set; } = false;

		[QueryStringParameter("id")]
		public string ReportId { get; set; }

		[QueryStringParameter("fi")]
		public string FilterImplementationText { get; set; } = string.Empty;

		[QueryStringParameter("fp")]
		public string FilterParallelismText
		{
			get => JoinFilterValueString(!FilterParallelSinglethreaded, "st", !FilterParallelMultithreaded, "mt");

			set
			{
				var values = SplitFilterValueString(value);

				FilterParallelSinglethreaded = !values.Contains("st");
				FilterParallelMultithreaded = !values.Contains("mt");
			}
		}

		[QueryStringParameter("fa")]
		public string FilterAlgorithmText
		{
			get => JoinFilterValueString(!FilterAlgorithmBase, "ba", !FilterAlgorithmWheel, "wh", !FilterAlgorithmOther, "ot");

			set
			{
				var values = SplitFilterValueString(value);

				FilterAlgorithmBase = !values.Contains("ba");
				FilterAlgorithmWheel = !values.Contains("wh");
				FilterAlgorithmOther = !values.Contains("ot");
			}
		}

		[QueryStringParameter("ff")]
		public string FilterFaithfulText
		{
			get => JoinFilterValueString(!FilterFaithful, "ff", !FilterUnfaithful, "uf");

			set
			{
				var values = SplitFilterValueString(value);

				FilterFaithful = !values.Contains("ff");
				FilterUnfaithful = !values.Contains("uf");
			}
		}

		[QueryStringParameter("fb")]
		public string FilterBitsText
		{
			get => JoinFilterValueString(!FilterBitsUnknown, "uk", !FilterBitsOne, "on", !FilterBitsOther, "ot");

			set
			{
				var values = SplitFilterValueString(value);

				FilterBitsUnknown = !values.Contains("uk");
				FilterBitsOne = !values.Contains("on");
				FilterBitsOther = !values.Contains("ot");
			}
		}

		public IList<string> FilterImplementations
			=> SplitFilterValueString(FilterImplementationText);

		public bool FilterParallelSinglethreaded { get; set; } = true;
		public bool FilterParallelMultithreaded { get; set; } = true;

		public bool FilterAlgorithmBase { get; set; } = true;
		public bool FilterAlgorithmWheel { get; set; } = true;
		public bool FilterAlgorithmOther { get; set; } = true;

		public bool FilterFaithful { get; set; } = true;
		public bool FilterUnfaithful { get; set; } = true;

		public bool FilterBitsUnknown { get; set; } = true;
		public bool FilterBitsOne { get; set; } = true;
		public bool FilterBitsOther { get; set; } = true;

		private bool AreFiltersClear
			=> FilterImplementationText == string.Empty
				&& FilterParallelismText == string.Empty
				&& FilterAlgorithmText == string.Empty
				&& FilterFaithfulText == string.Empty
				&& FilterBitsText == string.Empty;

		private string ReportTitle
		{
			get
			{
				StringBuilder titleBuilder = new();

				if (report?.User != null)
					titleBuilder.Append($" by {report.User}");

				if (report?.Date != null)
					titleBuilder.Append($" at {report.Date.Value.ToLocalTime()}");

				return titleBuilder.Length > 0 ? $"Report generated{titleBuilder}" : "Report";
			}
		}

		private Table<Result> resultTable;
		private Report report = null;
		private int rowNumber = 0;
		private Dictionary<string, LanguageInfo> languageMap = null;
		private bool processTableSortingChange = false;
		private List<ResultFilterPreset> filterPresets = null;
		private string filterPresetName;

		private ElementReference implementationsSelect;

		protected override async Task OnInitializedAsync()
		{
			report = await ReportReader.GetReport(ReportId);
			await LoadLanguageMap();

			if (LocalStorage.ContainKey(FilterPresetStorageKey))
			{
				try
				{
					filterPresets = LocalStorage.GetItem<List<ResultFilterPreset>>(FilterPresetStorageKey);
				}
				catch
				{
					LocalStorage.RemoveItem(FilterPresetStorageKey);
				}
			}

			await base.OnInitializedAsync();
		}

		public override Task SetParametersAsync(ParameterView parameters)
		{
			this.SetParametersFromQueryString(NavigationManager, LocalStorage);

			return base.SetParametersAsync(parameters);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			(string sortColumn, bool sortDescending) = resultTable.GetSortParameterValues();

			if (!processTableSortingChange && (!SortColumn.EqualsIgnoreCaseOrNull(sortColumn) || SortDescending != sortDescending))
			{
				if (resultTable.SetSortParameterValues(SortColumn, SortDescending))
					await resultTable.UpdateAsync();
			}

			UpdateQueryString();

			await base.OnAfterRenderAsync(firstRender);
		}

		private async Task ClearFilters()
		{
			FilterImplementationText = string.Empty;
			FilterParallelismText = string.Empty;
			FilterAlgorithmText = string.Empty;
			FilterFaithfulText = string.Empty;
			FilterBitsText = string.Empty;

			await JSRuntime.InvokeVoidAsync("PrimeViewJS.ClearMultiselectValues", implementationsSelect);
		}

		private void ToggleSystemInfoPanel()
		{
			HideSystemInformation = !HideSystemInformation;
		}

		private void ToggleFilterPanel()
		{
			HideFilters = !HideFilters;
		}

		private void ToggleFilterPresetPanel()
		{
			HideFilterPresets = !HideFilterPresets;
		}


		private async Task LoadLanguageMap()
		{
			try
			{
				languageMap = await Http.GetFromJsonAsync<Dictionary<string, LanguageInfo>>("data/langmap.json");
				foreach (var entry in languageMap)
					entry.Value.Key = entry.Key;
			}
			catch	{}
		}

		private void OnTableRefreshStart()
		{
			rowNumber = resultTable.PageNumber * resultTable.PageSize;

			if (!processTableSortingChange)
				return;

			(string sortColumn, bool sortDescending) = resultTable.GetSortParameterValues();

			processTableSortingChange = false;

			bool queryStringUpdateRequired = false;

			if (!sortColumn.EqualsIgnoreCaseOrNull(SortColumn))
			{
				SortColumn = sortColumn;
				queryStringUpdateRequired = true;
			}

			if (sortDescending != SortDescending)
			{
				SortDescending = sortDescending;
				queryStringUpdateRequired = true;
			}

			if (queryStringUpdateRequired)
				UpdateQueryString();
		}

		private LanguageInfo GetLanguageInfo(string language)
			=> languageMap != null && languageMap.ContainsKey(language) ? languageMap[language] : new() { Key = language, Name = language[0].ToString().ToUpper() + language[1..] };

		private void UpdateQueryString()
			=> this.UpdateQueryString(NavigationManager, LocalStorage, JSRuntime);

		private async Task ImplementationSelectionChanged(EventArgs args)
		{
			FilterImplementationText = await JSRuntime.InvokeAsync<string>("PrimeViewJS.GetMultiselectValues", implementationsSelect, "~") ?? string.Empty;
		}

		private string JoinFilterValueString(params object[] flagSet)
		{
			List<string> setFlags = new(); 

			for (int i = 0; i < flagSet.Length; i += 2)
			{
				if ((bool)flagSet[i])
					setFlags.Add(flagSet[i + 1].ToString());
			}

			return setFlags.Count > 0 ? string.Join("~", setFlags) : string.Empty;
		}

		private IList<string> SplitFilterValueString(string text)
			=> text.Split("~", StringSplitOptions.RemoveEmptyEntries);

		private async Task ApplyFilterPreset(int index)
		{
			var preset = filterPresets?[index];

			if (preset == null)
				return;

			FilterAlgorithmText = preset.AlgorithmText;
			FilterBitsText = preset.BitsText;
			FilterFaithfulText = preset.FaithfulText;
			FilterImplementationText = preset.ImplementationText;
			FilterParallelismText = preset.ParallelismText;

			var filterImplementations = FilterImplementations;

			if (filterImplementations.Count > 0)
				await JSRuntime.InvokeVoidAsync("PrimeViewJS.SetMultiselectValues", implementationsSelect, FilterImplementations.ToArray());

			else
				await JSRuntime.InvokeVoidAsync("PrimeViewJS.ClearMultiselectValues", implementationsSelect);

			filterPresetName = preset.Name;
		}

		private void RemoveFilterPreset(int index)
		{
			filterPresets?.RemoveAt(index);

			LocalStorage.SetItem(FilterPresetStorageKey, filterPresets);
		}

		private void AddFilterPreset()
		{
			if (string.IsNullOrWhiteSpace(filterPresetName))
				return;

			if (filterPresets == null)
				filterPresets = new();

			int i;
			for (i = 0; i < filterPresets.Count && string.Compare(filterPresetName, filterPresets[i].Name, StringComparison.OrdinalIgnoreCase) > 0; i++);

			if (i < filterPresets.Count && string.Equals(filterPresetName, filterPresets[i].Name, StringComparison.OrdinalIgnoreCase))
				filterPresets.RemoveAt(i);

			filterPresets.Insert(i, new()
			{
				Name = filterPresetName,
				AlgorithmText = FilterAlgorithmText,
				BitsText = FilterBitsText,
				FaithfulText = FilterFaithfulText,
				ImplementationText = FilterImplementationText,
				ParallelismText = FilterParallelismText
			});

			LocalStorage.SetItem(FilterPresetStorageKey, filterPresets);

			filterPresetName = null;
		}
	}
}
