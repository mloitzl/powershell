﻿namespace PnP.PowerShell.Commands.Base.PipeBinds
{
    public class FileVersionPipeBind
    {
        private int _id = -1;
        private string _label;

        public FileVersionPipeBind(string label)
        {
            if (int.TryParse(label, out int id))
            {
                _id = id;
            }
            else
            {
                _label = label;
            }
        }

        public FileVersionPipeBind(int id)
        {
            _id = id;
        }

        public int Id => _id;
        public string Label => _label;
    }
}