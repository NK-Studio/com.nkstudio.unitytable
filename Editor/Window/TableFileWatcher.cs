using System;
using System.IO;
using System.Threading;
using UnityEditor;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 파일 하나를 감시해, 에디터 밖에서 바뀌면 메인 스레드에서 한 번 알립니다.
    /// FileSystemWatcher 이벤트는 백그라운드 스레드에서 오므로 여기서는 시각만 기록하고,
    /// EditorApplication.update에서 조용해진 것을 확인한 뒤 Changed를 부릅니다.
    /// </summary>
    internal sealed class TableFileWatcher : IDisposable
    {
        // 편집기는 한 번 저장할 때도 쓰기·크기 변경·이름 바꾸기 이벤트를 여러 번 낸다. 마지막 이벤트 뒤
        // 이만큼 조용하면 저장이 끝난 것으로 본다. 300ms는 일반 저장이 끝나기에 충분하고, 반영이 늦다고 느끼지 않는 값이다.
        private static readonly long DebounceTicks = TimeSpan.FromMilliseconds(300).Ticks;

        private readonly FileSystemWatcher _watcher;
        private readonly Action _changed;

        private long _lastEventTicks;
        private int _hasPendingEvent;

        /// <summary>
        /// 감시를 시작합니다.
        /// </summary>
        /// <param name="fullPath">감시할 파일의 절대 경로입니다.</param>
        /// <param name="changed">파일이 바뀌었을 때 메인 스레드에서 부를 콜백입니다.</param>
        public TableFileWatcher(string fullPath, Action changed)
        {
            _changed = changed;

            // 같은 폴더의 다른 파일 이벤트는 받지 않도록 파일 이름으로 거른다.
            // 임시 파일에 쓴 뒤 원본 이름으로 바꾸는 저장 방식은 Renamed(새 이름 = 원본)로 잡힌다.
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = false,
            };

            _watcher.Changed += OnFileEvent;
            _watcher.Created += OnFileEvent;
            _watcher.Renamed += OnFileEvent;
            _watcher.EnableRaisingEvents = true;

            EditorApplication.update += Poll;
        }

        /// <summary>
        /// 감시 중인 파일의 절대 경로입니다.
        /// </summary>
        public string FullPath => Path.Combine(_watcher.Path, _watcher.Filter);

        /// <summary>
        /// 지금 읽을 수 없었던 경우(다른 프로그램이 쓰는 중 등) 잠시 뒤 다시 알리도록 예약합니다.
        /// </summary>
        public void RequestRecheck()
        {
            MarkPending();
        }

        public void Dispose()
        {
            EditorApplication.update -= Poll;

            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileEvent;
            _watcher.Created -= OnFileEvent;
            _watcher.Renamed -= OnFileEvent;
            _watcher.Dispose();
        }

        // 백그라운드 스레드에서 호출된다. Unity API를 부르지 않는다.
        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            MarkPending();
        }

        private void MarkPending()
        {
            Interlocked.Exchange(ref _lastEventTicks, DateTime.UtcNow.Ticks);
            Interlocked.Exchange(ref _hasPendingEvent, 1);
        }

        private void Poll()
        {
            if (Volatile.Read(ref _hasPendingEvent) == 0)
                return;

            if (DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastEventTicks) < DebounceTicks)
                return;

            Interlocked.Exchange(ref _hasPendingEvent, 0);
            _changed?.Invoke();
        }
    }
}
