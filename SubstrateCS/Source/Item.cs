﻿using System;
using System.Collections.Generic;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate
{
    /// <summary>
    /// Represents an item (or item stack) within an item slot.
    /// </summary>
    public class Item : INbtObject<Item>, ICopyable<Item>
    {
        private static readonly SchemaNodeCompound _schema = new SchemaNodeCompound("")
        {
            // TODO!!! id can now be either string or int, not sure how to handle this for validation cleanly
            //new SchemaNodeScaler("id", TagType.TAG_STRING),
            new SchemaNodeScaler("Damage", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
            new SchemaNodeScaler("Count", TagType.TAG_BYTE),
            new SchemaNodeCompound("tag", new SchemaNodeCompound("") {
                new SchemaNodeList("ench", TagType.TAG_COMPOUND, Enchantment.Schema, SchemaOptions.OPTIONAL),
                new SchemaNodeScaler("title", TagType.TAG_STRING, SchemaOptions.OPTIONAL),
                new SchemaNodeScaler("author", TagType.TAG_STRING, SchemaOptions.OPTIONAL),
                new SchemaNodeList("pages", TagType.TAG_STRING, SchemaOptions.OPTIONAL),
            }, SchemaOptions.OPTIONAL),
        };

        private TagNodeCompound _source;

        private string _id;
        private int _intId;

        private byte _count;
        private short _damage;

        private List<Enchantment> _enchantments;

        /// <summary>
        /// Constructs an empty <see cref="Item"/> instance.
        /// </summary>
        public Item ()
        {
            _enchantments = new List<Enchantment>();
            _source = new TagNodeCompound();
        }

        /// <summary>
        /// Constructs an <see cref="Item"/> instance representing the given item id.
        /// </summary>
        /// <param name="id">An item id.</param>
        public Item (string id)
            : this()
        {
            _id = id;
        }

        #region Properties

        /// <summary>
        /// Gets an <see cref="ItemInfo"/> entry for this item's type.
        /// </summary>
        // TODO!!! Disable for now. Item ID is a string in latest version, but this is based around integers
        /*public ItemInfo Info
        {
            get { return ItemInfo.ItemTable[_id]; }
        }*/

        /// <summary>
        /// Gets or sets the current type (id) of the item.
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the damage value of the item.
        /// </summary>
        /// <remarks>The damage value may represent a generic data value for some items.</remarks>
        public int Damage
        {
            get { return _damage; }
            set { _damage = (short)value; }
        }

        /// <summary>
        /// Gets or sets the number of this item stacked together in an item slot.
        /// </summary>
        public int Count
        {
            get { return _count; }
            set { _count = (byte)value; }
        }

        /// <summary>
        /// Gets the list of <see cref="Enchantment"/>s applied to this item.
        /// </summary>
        public IList<Enchantment> Enchantments
        {
            get { return _enchantments; }
        }

        /// <summary>
        /// Gets the source <see cref="TagNodeCompound"/> used to create this <see cref="Item"/> if it exists.
        /// </summary>
        public TagNodeCompound Source
        {
            get { return _source; }
        }

        /// <summary>
        /// Gets a <see cref="SchemaNode"/> representing the schema of an item.
        /// </summary>
        public static SchemaNodeCompound Schema
        {
            get { return _schema; }
        }

        #endregion

        #region ICopyable<Item> Members

        /// <inheritdoc/>
        public Item Copy ()
        {
            Item item = new Item();
            item._id = _id;
            item._count = _count;
            item._damage = _damage;

            foreach (Enchantment e in _enchantments) {
                item._enchantments.Add(e.Copy());
            }

            if (_source != null) {
                item._source = _source.Copy() as TagNodeCompound;
            }

            return item;
        }

        #endregion

        #region INBTObject<Item> Members

        /// <inheritdoc/>
        public Item LoadTree (TagNode tree)
        {
            TagNodeCompound ctree = tree as TagNodeCompound;
            if (ctree == null) {
                return null;
            }

            _enchantments.Clear();

            var idNode = ctree["id"];

            if(idNode.IsCastableTo(TagType.TAG_INT))
            {
                _id = null;
                _intId = idNode.ToTagInt();
            }
            else
            {
                _id = idNode.ToTagString();
                _intId = -1;
            }

            _count = ctree["Count"].ToTagByte();

            if(ctree.ContainsKey("Damage"))
                _damage = ctree["Damage"].ToTagShort();

            if (ctree.ContainsKey("tag")) {
                TagNodeCompound tagtree = ctree["tag"].ToTagCompound();
                if (tagtree.ContainsKey("ench")) {
                    TagNodeList enchList = tagtree["ench"].ToTagList();

                    foreach (TagNode tag in enchList) {
                        _enchantments.Add(new Enchantment().LoadTree(tag));
                    }
                }
            }

            _source = ctree.Copy() as TagNodeCompound;

            return this;
        }

        /// <inheritdoc/>
        public Item LoadTreeSafe (TagNode tree)
        {
            if (!ValidateTree(tree)) {
                return null;
            }

            return LoadTree(tree);
        }

        /// <inheritdoc/>
        public TagNode BuildTree ()
        {
            TagNodeCompound tree = new TagNodeCompound();

            if (_id != null)
                tree["id"] = new TagNodeString(_id);
            else
                tree["id"] = new TagNodeInt(_intId);

            tree["Count"] = new TagNodeByte(_count);
            tree["Damage"] = new TagNodeShort(_damage);

            if (_enchantments.Count > 0) {
                TagNodeList enchList = new TagNodeList(TagType.TAG_COMPOUND);
                foreach (Enchantment e in _enchantments) {
                    enchList.Add(e.BuildTree());
                }

                TagNodeCompound tagtree = new TagNodeCompound();
                tagtree["ench"] = enchList;

                if (_source != null && _source.ContainsKey("tag")) {
                    tagtree.MergeFrom(_source["tag"].ToTagCompound());
                }

                tree["tag"] = tagtree;
            }

            if (_source != null) {
                tree.MergeFrom(_source);
            }

            return tree;
        }

        /// <inheritdoc/>
        public bool ValidateTree (TagNode tree)
        {
            return new NbtVerifier(tree, _schema).Verify();
        }

        #endregion
    }
}
