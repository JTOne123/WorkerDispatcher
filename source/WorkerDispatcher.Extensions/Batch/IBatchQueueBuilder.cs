﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerDispatcher.Extensions.Batch
{
    public interface IBatchQueueBuilder
    {        
        IBatchBindingBuilder For<TData>();
    }
}
