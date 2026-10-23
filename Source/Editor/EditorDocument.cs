namespace Mario2.Editor
{
    /// <summary>
    /// The level being edited plus its undo history. Edits happen inside transactions: a whole
    /// drag stroke is one transaction, and therefore one undo step. Undo is snapshot based,
    /// which is cheap because tile specs and entity placements are immutable.
    /// </summary>
    public sealed class EditorDocument
    {
        private const int MaxUndoSteps = 200;

        private sealed record Snapshot(string Label, LevelData Level, int StateId);

        public LevelData Level { get; private set; }
        public string FilePath { get; set; }

        /// <summary>Increments on every change, including undo and redo.</summary>
        public int Version { get; private set; }

        public bool IsDirty => _stateId != _savedStateId;
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public string? UndoLabel => _undo.Last?.Value.Label;
        public string? RedoLabel => _redo.Count > 0 ? _redo.Peek().Label : null;
        public bool InTransaction => _txBefore is not null;

        public event Action? Changed;

        private readonly LinkedList<Snapshot> _undo = new();
        private readonly Stack<Snapshot> _redo = new();

        // Identifies the content state, so undoing back to the saved state clears the dirty flag
        private int _stateId;
        private int _savedStateId;
        private int _nextStateId = 1;

        private LevelData? _txBefore;
        private int _txBeforeStateId;
        private string _txLabel = string.Empty;
        private bool _txChanged;

        public EditorDocument(LevelData level, string filePath)
        {
            Level = level;
            FilePath = filePath;
        }

        public void Begin(string label)
        {
            if (InTransaction)
                Commit();

            _txBefore = Level.Clone();
            _txBeforeStateId = _stateId;
            _txLabel = label;
            _txChanged = false;
        }

        /// <summary>Call after mutating <see cref="Level"/>.</summary>
        public void MarkChanged()
        {
            _stateId = _nextStateId++;
            if (InTransaction)
                _txChanged = true;

            NotifyChanged();
        }

        public void Commit()
        {
            if (_txBefore is null)
                return;

            if (_txChanged)
            {
                _undo.AddLast(new Snapshot(_txLabel, _txBefore, _txBeforeStateId));
                if (_undo.Count > MaxUndoSteps)
                    _undo.RemoveFirst();

                _redo.Clear();
            }

            _txBefore = null;
        }

        public void Cancel()
        {
            if (_txBefore is null)
                return;

            if (_txChanged)
            {
                Level = _txBefore;
                _stateId = _txBeforeStateId;
                NotifyChanged();
            }

            _txBefore = null;
        }

        /// <summary>Runs a single edit as its own undo step. The edit returns whether it changed anything.</summary>
        public bool Execute(string label, Func<LevelData, bool> edit)
        {
            Begin(label);
            bool changed = edit(Level);
            if (changed)
                MarkChanged();

            Commit();
            return changed;
        }

        public bool Undo()
        {
            Commit();
            if (_undo.Last is null)
                return false;

            Snapshot snapshot = _undo.Last.Value;
            _undo.RemoveLast();
            _redo.Push(new Snapshot(snapshot.Label, Level, _stateId));
            Restore(snapshot);
            return true;
        }

        public bool Redo()
        {
            Commit();
            if (_redo.Count == 0)
                return false;

            Snapshot snapshot = _redo.Pop();
            _undo.AddLast(new Snapshot(snapshot.Label, Level, _stateId));
            Restore(snapshot);
            return true;
        }

        public void MarkSaved()
        {
            _savedStateId = _stateId;
            NotifyChanged();
        }

        /// <summary>Flags the document as having unsaved changes, without an undo step (e.g. after a migration).</summary>
        public void MarkUnsaved()
        {
            _stateId = _nextStateId++;
            NotifyChanged();
        }

        private void Restore(Snapshot snapshot)
        {
            Level = snapshot.Level;
            _stateId = snapshot.StateId;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
