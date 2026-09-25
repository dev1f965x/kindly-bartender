import "./BrandMark.css";

/**
 * The mark: a filled glass on a bar.
 *
 * Drawn inline so that it takes the palette's tokens and stays sharp at any window scale.
 */
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 40 40" role="img" aria-label="Kindly Bartender">
      <rect className="brand-mark__plate" x="0.75" y="0.75" width="38.5" height="38.5" rx="10.25" />
      <path className="brand-mark__glass" d="M11 13h18l-9 9z" />
      <path className="brand-mark__stem" d="M20 22v6" />
      <path className="brand-mark__base" d="M15.5 28h9" />
      <circle className="brand-mark__olive" cx="23" cy="15.2" r="1.8" />
    </svg>
  );
}
