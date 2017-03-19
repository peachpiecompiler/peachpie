using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Spl
{
    // TODO:
    // ArrayObject

    /// <summary>
    /// The SplObserver interface is used alongside <see cref="SplSubject"/> to implement the Observer Design Pattern.
    /// </summary>
    [PhpType("[name]")]
    public interface SplObserver
    {
        /// <summary>
        /// This method is called when any <see cref="SplSubject"/> to which the observer is attached calls <see cref="SplSubject.notify"/>.
        /// </summary>
        void update(SplSubject subject);
    }

    /// <summary>
    /// The SplSubject interface is used alongside <see cref="SplObserver"/> to implement the Observer Design Pattern.
    /// </summary>
    [PhpType("[name]")]
    public interface SplSubject
    {
        /// <summary>
        /// Attaches an <see cref="SplObserver"/> so that it can be notified of updates.
        /// </summary>
        void attach(SplObserver observer);

        /// <summary>
        /// Detaches an observer from the subject to no longer notify it of updates.
        /// </summary>
        void detach(SplObserver observer);

        /// <summary>
        /// Notifies all attached observers.
        /// </summary>
        void notify();
    }
}
