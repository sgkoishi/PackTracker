﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PackHistorian.Entity;

namespace PackHistorian {
  class History : IEnumerable<Pack> {
    List<Pack> _packs;

    public History() {
      _packs = new List<Pack>();
    }

    public History(IEnumerable<Pack> Packs) {
      _packs = Packs.ToList();
    }

    public History Ascending { get { return new History(_packs.OrderBy(x => x.Time)); } }

    public void Add(Pack Pack) {
      _packs.Add(Pack);
    }

    public IEnumerator<Pack> GetEnumerator() {
      return _packs.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return _packs.GetEnumerator();
    }
  }
}