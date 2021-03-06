﻿using JobView.Models;
using Microsoft.Win32.SafeHandles;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using static JobView.NativeMethods;

namespace JobView.ViewModels {
	class JobDetailsViewModel : BindableBase {
		JobObjectViewModel _job;
		IntPtr? _jobHadle;
		IMainViewModel _mainViewModel;

		public DelegateCommandBase GoToJobCommand { get; }

		public JobDetailsViewModel(IMainViewModel mainViewModel) {
			_mainViewModel = mainViewModel;

			GoToJobCommand = new DelegateCommand<JobObjectViewModel>(async job => {
				await Dispatcher.CurrentDispatcher.InvokeAsync(() => _job.IsExpanded = true);
				_mainViewModel.SelectedJob = job;
			});
		}

		public bool IsJobSelected => _job != null;

		public JobObjectViewModel Job {
			get { return _job; }
			set {
				SetProperty(ref _job, value);

				_jobHadle = _job?.Job.Handle;

				_processes = null;

				// refresh all properties
				RaisePropertyChanged(nameof(Name));
				RaisePropertyChanged(nameof(Address));
				RaisePropertyChanged(nameof(ChildJobs));
				RaisePropertyChanged(nameof(IsJobSelected));
				RaisePropertyChanged(nameof(ParentJob));
				RaisePropertyChanged(nameof(Processes));
				RaisePropertyChanged(nameof(JobInformation));
				RaisePropertyChanged(nameof(JobId));
			}
		}

		public string Name => _job?.Name;
		public ulong? Address => _job?.Address;

		public int? JobId => _job?.JobId;

		public IList<JobObjectViewModel> ChildJobs => _job?.ChildJobs;
		public JobObjectViewModel ParentJob => _job?.ParentJob == null ? null : _mainViewModel.GetJobByAddress(_job.ParentJob.Address);

		ProcessViewModel[] _processes;
		public unsafe ProcessViewModel[] Processes {
			get {
				if (_processes == null) {
					if (_job == null)
						return null;

					JobBasicProcessIdList list;
					if (QueryInformationJobObject(_jobHadle.Value, JobInformationClass.BasicProcessList, out list, Marshal.SizeOf<JobBasicProcessIdList>())) {
						_processes = list.ProcessIds.Take(list.ProcessesInList).Select(id => new ProcessViewModel {
							Id = id.ToInt32(),
							Name = Process.GetProcessById(id.ToInt32())?.ProcessName
						}).OrderBy(process => process.Name).ToArray();
						_job.ProcessCount = _processes.Length;
					}
				}
				return _processes;
			}
		}

		public JobObjectInformation JobInformation => _job?.JobInformation;

	}
}
