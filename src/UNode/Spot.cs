/*
 * This file is part of the UNode package.
 *
 * (c) MuGuangyi <muguangyi@hotmail.com>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UNode
{
    public abstract class Spot : IDisposable
    {
        private string name = null;
        private List<Link> links = new List<Link>();
        private Node.NodeMessage signalMessage = null;

        public Spot(Node owner, int capacity, SpotType type)
        {
            this.ID = Guid.NewGuid().ToString();
            this.Owner = owner;
            this.Capacity = capacity;
            this.Type = type;
            this.signalMessage = new Node.NodeMessage(Node.NodeMessage.SIGNAL, this);
        }

        public void Dispose()
        {
            if (null != this.links)
            {
                while (this.links.Count > 0)
                {
                    this.links[0].Dispose();
                }
                this.links = null;
            }

            this.Owner = null;
            this.signalMessage = null;
        }

        public string ID { get; set; } = null;

        public virtual string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public Node Owner { get; private set; } = null;

        public int Capacity { get; } = -1;

        public SpotType Type { get; } = SpotType.In;

        public int LinkCount
        {
            get
            {
                return this.links.Count;
            }
        }

        public bool IsHidden
        {
            get
            {
                return this.Owner.IsSpotHidden(this);
            }
        }

        public bool TryConnect(Spot spot)
        {
            if (this == spot || this.Owner == spot.Owner)
            {
                return false;
            }

            if (0 != (this.Type & spot.Type))
            {
                return false;
            }

            return (CanConnect(spot) && spot.CanConnect(this));
        }

        public void Connect(Link link)
        {
            if (-1 != this.Capacity && this.links.Count == this.Capacity)
            {
                var index = this.links.Count - 1;
                this.links[index].Dispose();
            }

            this.links.Add(link);
        }

        public void Disconnect(Link link)
        {
            var index = this.links.IndexOf(link);
            if (index >= 0)
            {
                this.links.RemoveAt(index);
            }
        }

        public Link GetLinkAt(int index)
        {
            if (index >= 0 && index < this.links.Count)
            {
                return this.links[index];
            }

            return null;
        }

        public void Signal(params object[] args)
        {
            NotifySignal();
            for (var i = 0; i < this.links.Count; ++i)
            {
                var link = this.links[i];
                var spot = link.GetLinkedSpot(this);
                spot.OnSignal(args);
            }
        }

        protected abstract bool CanConnect(Spot spot);

        private void OnSignal(params object[] args)
        {
            NotifySignal();
            this.Owner.OnSignal(this, args);
        }

        private void NotifySignal()
        {
            if (Application.isEditor && NodeVM.IsDebugging)
            {
                this.Owner.Dispatcher.Notify(this.signalMessage);
            }
        }
    }
}